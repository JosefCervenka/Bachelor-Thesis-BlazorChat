using BlazorChatApp.Application.DTOs.ChatRequests;
using BlazorChatApp.Application.DTOs.ChatRooms;
using BlazorChatApp.Application.DTOs.Messages;
using BlazorChatApp.Application.Services.SignalRServices;
using BlazorChatApp.Application.Utils.Observers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BlazorChatApp.Application.Services.ChatServices
{
    public class ChatRoomService
    {
        private bool isInitialized = false;

        protected readonly HttpClient _httpClient;

        protected readonly ISignalRService _chatService;

        protected readonly NavigationManager _navigationManager;

        private Dictionary<Guid, ChatRoomDTO> _chatRoomDTOs = [];

        private Dictionary<Guid, (int Count, DateTime TimeStamp)> _unreadMessages = new Dictionary<Guid, (int Count, DateTime TimeStamp)>();

        protected readonly Observer _chatObserver;
        private HubConnection HubConnection { get; set; }

        public ChatRoomService(HttpClient httpClient, ISignalRService chatService, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _chatService = chatService;
            _navigationManager = navigationManager;
            _chatObserver = new Observer();
        }

        public async Task NotifyVisited(Guid? chatRoomId)
        {
            if (chatRoomId is null)
                return;

            await Initialization();

            await HubConnection.InvokeAsync("LastVisited", chatRoomId);
            _unreadMessages[(Guid)chatRoomId] = (0, DateTime.Now);
            _chatObserver.Notify();
        }

        public async Task<List<ChatRoomDTO>> GetChatRooms()
        {
            return _chatRoomDTOs
                .Select(x => x.Value).ToList();
        }

        public async Task<Dictionary<Guid, (int Count, DateTime TimeStamp)>> GetUnreadMessagesCount()
        {
            return _unreadMessages;
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
                ChatRoomWrapperDTO chatRoomWrapper = await _httpClient.GetFromJsonAsync<ChatRoomWrapperDTO>("/api/chatRoom/chats", jsonOptions);
                _chatRoomDTOs = chatRoomWrapper?.ChatRooms?.ToDictionary(x => x.Id) ?? [];

                UnreadMessageCountWrapperDTO unreadMessageCountWrapper = await _httpClient.GetFromJsonAsync<UnreadMessageCountWrapperDTO>("/api/message/unread-messages-count", jsonOptions);
                _unreadMessages = unreadMessageCountWrapper?.UnreadCounts?.ToDictionary(x => x.ChatId, x => (x.MessageCount, x.TimeStamp)) ?? [];
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _chatRoomDTOs = [];
                _unreadMessages = new Dictionary<Guid, (int Count, DateTime TimeStamp)>();
            }

            await StartConnectionHub();
        }

        private async Task StartConnectionHub()
        {
            HubConnection = await _chatService.CreateConnection("chatroom");

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
            HubConnection.On("ChatAccepted", (ChatRoomDTO chatRoomDTO) =>
            {
                lock (_chatRoomDTOs)
                {
                    _chatRoomDTOs!.TryAdd(chatRoomDTO.Id, chatRoomDTO);
                }

                _chatObserver.Notify();
            });

            HubConnection.On("ChatChanged", (ChatRoomDTO chatRoomDTO) =>
            {
                lock (_chatRoomDTOs)
                {
                    _chatRoomDTOs[chatRoomDTO.Id] = chatRoomDTO;
                }
                _chatObserver.Notify();
            });

            HubConnection.On("ChatUserRemove", (Guid chatRoomId) =>
            {
                lock (_chatRoomDTOs)
                {
                    _chatRoomDTOs.Remove(chatRoomId);
                }

                var currentUri = _navigationManager.ToBaseRelativePath(_navigationManager.Uri);

                if (currentUri.StartsWith($"group-chatroom/{chatRoomId}", StringComparison.OrdinalIgnoreCase) ||
                    currentUri.StartsWith($"chatroom/{chatRoomId}", StringComparison.OrdinalIgnoreCase))
                {
                    _navigationManager.NavigateTo("/");
                }

                _chatObserver.Notify();
            });

            HubConnection.On("MessageNotification", (Guid chatRoomId, DateTime timeStamp) =>
            {
                if (_unreadMessages.ContainsKey(chatRoomId))
                {
                    if (_unreadMessages[chatRoomId].TimeStamp < timeStamp)
                        _unreadMessages[chatRoomId] = (_unreadMessages[chatRoomId].Count + 1, timeStamp);
                }
                else
                {
                    _unreadMessages[chatRoomId] = (1, timeStamp);
                }
                _chatObserver.Notify();
            });
        }

        public async Task AddMemberToGroupChatRoom(GroupChatRequestDTO groupChatRequestDTO) =>
            await _httpClient.PostAsJsonAsync("/api/invitation/create", (ChatRequestDTO)groupChatRequestDTO);


        public async Task<GroupChatRoomDTO?> CreateGroupChatRoom(GroupChatRoomDTO groupChatRoomDTO)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chatroom/chat", groupChatRoomDTO);

            if (!response.IsSuccessStatusCode)
                return null;

            try
            {
                var result = await response.Content.ReadFromJsonAsync<GroupChatRoomDTO>(new JsonSerializerOptions
                {
                    ReferenceHandler = ReferenceHandler.Preserve,
                    PropertyNamingPolicy = null,
                    PropertyNameCaseInsensitive = true
                });

                if (result == null)
                    return null;

                foreach (var invitedMember in groupChatRoomDTO.ChatMembers)
                {
                    GroupChatRequestDTO groupChatRequestDTO = new GroupChatRequestDTO()
                    {
                        InvitedId = invitedMember.UserId,
                        GroupChatRoomId = result.Id
                    };

                    await _httpClient.PostAsJsonAsync("/api/invitation/create", (ChatRequestDTO)groupChatRequestDTO);
                }

                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error deserializing GroupChatRoomDTO: {e.Message}");
                return null;
            }
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
