using BlazorChatApp.Application.DTOs.Messages;
using BlazorChatApp.Application.Services.SignalRServices;
using BlazorChatApp.Application.Utils.Observers;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorChatApp.Application.Services.ChatServices
{
    public class ChatService
    {
        protected readonly ISignalRService _chatService;

        private readonly HttpClient _httpClient;

        public Dictionary<Guid, List<MessageDTO>> _messagesByRoom = [];

        protected readonly Observer _chatObserver;
        private HubConnection HubConnection { get; set; }

        public ChatService(ISignalRService signalRService, HttpClient httpClient)
        {
            _chatService = signalRService;
            _httpClient = httpClient;
            _chatObserver = new Observer();
        }

        public async Task Inicilize(Guid chatRoomId)
        {
            if (!_messagesByRoom.TryGetValue(chatRoomId, out var _))
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = null,
                    ReferenceHandler = ReferenceHandler.Preserve,
                };

                var wrapper = await _httpClient.GetFromJsonAsync<MessageWrapperDTO>($"/api/message/{chatRoomId}", jsonOptions);

                var messages = wrapper.Messages.ToList();

                if (wrapper?.Messages is not null)
                {
                    _messagesByRoom.Add(chatRoomId, messages);
                }

                await StartConnectionHub(chatRoomId);
            }
        }

        public async Task<bool> LoadAditionalMessages(Guid chatRoomId, int numberOfMessages = 50)
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = null,
                ReferenceHandler = ReferenceHandler.Preserve,
            };
            if (_messagesByRoom.TryGetValue(chatRoomId, out var existingMessages))
            {
                var wrapper = await _httpClient.GetFromJsonAsync<MessageWrapperDTO>($"/api/message/{chatRoomId}?skip={existingMessages.Count}&numberOfMessages={numberOfMessages}", jsonOptions);

                var messages = wrapper.Messages.ToList();

                _messagesByRoom[chatRoomId] = [.. messages, .. existingMessages];

                if (messages.Count == numberOfMessages)
                {
                    return true;
                }
            }

            return false;
        }

        private async Task StartConnectionHub(Guid chatRoomId)
        {
            HubConnection = await _chatService.CreateConnection($"chat?chatRoomId={chatRoomId}");

            RegiseterConnectionHubHandler();

            await HubConnection.StartAsync();

        }

        private void RegiseterConnectionHubHandler()
        {
            HubConnection.On("ReceiveMessage", (MessageDTO messageDTO) =>
            {
                lock (_messagesByRoom)
                {
                    _messagesByRoom.TryGetValue(messageDTO?.ChatRoom?.Id ?? Guid.Empty, out var messages);

                    if (messages is not null)
                    {
                        messages.Add(messageDTO);
                    }
                }
                _chatObserver.Notify();
            });
        }

        public async Task<List<MessageDTO>> GetMessages(Guid chatRoomId)
        {
            _messagesByRoom.TryGetValue(chatRoomId, out var messages);

            return messages ?? [];
        }

        public async Task SendMessage(MessageDTO messageDTO)
        {
            if (messageDTO is PhotoMessageDTO photoMessageDTO)
                await HubConnection.SendAsync("SendMessage", photoMessageDTO);
            else
                await HubConnection.SendAsync("SendMessage", messageDTO);

        }

        public void RegisterObserver(Action observer)
        {
            _chatObserver.RegisterObserver(observer);
        }

        public void UnregisterObserver(Action observer)
        {
            _chatObserver.UnregisterObserver(observer);
        }
    }
}
