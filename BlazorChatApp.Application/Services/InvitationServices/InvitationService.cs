using BlazorChatApp.Application.DTOs.ChatRequests;
using BlazorChatApp.Application.Services.SignalRServices;
using BlazorChatApp.Application.Utils.Observers;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorChatApp.Application.Services.InvitationServices
{
    public class InvitationService
    {
        private bool isInitialized = false;

        protected readonly HttpClient _httpClient;

        protected readonly ISignalRService _chatService;

        private Dictionary<Guid, ChatRequestDTO> _chatRequestsInvited = [];

        private Dictionary<Guid, ChatRequestDTO> _chatRequestsInitialized = [];

        protected readonly Observer _invitationObserver;
        private HubConnection HubConnection { get; set; }

        public InvitationService(HttpClient httpClient, ISignalRService chatService)
        {
            _httpClient = httpClient;
            _chatService = chatService;
            _invitationObserver = new Observer();
        }

        public async Task<List<ChatRequestDTO>> GetChatRequestsInvited()
        {
            return _chatRequestsInvited
                .Select(x => x.Value).ToList();
        }

        public async Task<List<ChatRequestDTO>> GetChatRequestsInitialized()
        {
            return _chatRequestsInitialized
                .Select(x => x.Value).ToList();
        }

        public async Task Initialization()
        {
            if (isInitialized)
                return;

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null,
                ReferenceHandler = ReferenceHandler.Preserve,
            };

            try
            {
                ChatRequestWrapperDTO wrapper = await _httpClient.GetFromJsonAsync<ChatRequestWrapperDTO>("/api/invitation/invited", jsonOptions);
                _chatRequestsInvited = wrapper?.ChatRequests?.ToDictionary(x => x.Id) ?? [];
            }
            catch (Exception)
            {
                _chatRequestsInvited = [];
            }

            try
            {
                ChatRequestWrapperDTO wrapper = await _httpClient.GetFromJsonAsync<ChatRequestWrapperDTO>("/api/invitation/initialized", jsonOptions);
                _chatRequestsInitialized = wrapper?.ChatRequests?.ToDictionary(x => x.Id) ?? [];
            }
            catch (Exception)
            {
                _chatRequestsInitialized = [];
            }

            await StartConnectionHub();
        }

        public async Task<(bool Success, string? ErrorMessage)> SendRequest(ChatRequestDTO chatRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/invitation/create", chatRequest);

            if (response.IsSuccessStatusCode)
            {
                return (true, null);
            }

            try
            {
                var errorResponse = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return (false, errorResponse?.Message ?? "Failed to send invitation.");
            }
            catch
            {
                return (false, "Failed to send invitation.");
            }
        }

        public async Task ResponseRequest(ChatRequestDTO chatRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/invitation/response", chatRequest);
        }

        public async Task RemoveRequest(ChatRequestDTO chatRequest)
        {
            var response = await _httpClient.DeleteAsync($"/api/invitation/{chatRequest.Id}");
        }

        private async Task StartConnectionHub()
        {
            HubConnection = await _chatService.CreateConnection("notification");

            RegiseterConnectionHubHandler();

            try
            {
                await HubConnection.StartAsync();
            }
            catch (Exception)
            {

            }
        }

        private void RegiseterConnectionHubHandler()
        {
            HubConnection.On("ChatRequestInvited", (ChatRequestDTO chatRequest) =>
            {
                lock (_chatRequestsInvited)
                {
                    _chatRequestsInvited!.TryAdd(chatRequest.Id, chatRequest);
                }
                _invitationObserver.Notify();
            });

            HubConnection.On("ChatRequestInitialized", (ChatRequestDTO chatRequest) =>
            {
                lock (_chatRequestsInitialized)
                {
                    _chatRequestsInitialized!.TryAdd(chatRequest.Id, chatRequest);
                }
                _invitationObserver.Notify();
            });

            HubConnection.On("ChatRequestResponseInvited", (ChatRequestDTO chatRequest) =>
            {
                lock (_chatRequestsInvited)
                {
                    _chatRequestsInvited!.Remove(chatRequest.Id, out _);
                }
                _invitationObserver.Notify();
            });

            HubConnection.On("ChatRequestResponseInitialized", (ChatRequestDTO chatRequest) =>
            {
                lock (_chatRequestsInitialized)
                {
                    _chatRequestsInitialized!.Remove(chatRequest.Id, out _);
                }
                _invitationObserver.Notify();
            });

            HubConnection.On("GroupChatRequestRemoved", (ChatRequestDTO chatRequest) =>
            {
                lock (_chatRequestsInvited)
                {
                    _chatRequestsInvited!.Remove(chatRequest.Id, out _);
                    _chatRequestsInitialized.Remove(chatRequest.Id, out _);
                }
                _invitationObserver.Notify();
            });
        }

        public void RegisterObserver(Action observer)
        {
            _invitationObserver.RegisterObserver(observer);
        }

        public void UnregisterObserver(Action observer)
        {
            _invitationObserver.UnregisterObserver(observer);
        }

        private class ErrorResponse
        {
            public string? Message { get; set; }
        }
    }
}
