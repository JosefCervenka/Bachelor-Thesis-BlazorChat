using BlazorChatApp.Domain.Models.Chats;

namespace BlazorChatApp.Domain.Models.ChatRequests
{
    public class GroupChatRequest : ChatRequest
    {
        public Guid GroupChatRoomId { get; set; }
        public GroupChatRoom GroupChatRoom { get; set; }
    }
}
