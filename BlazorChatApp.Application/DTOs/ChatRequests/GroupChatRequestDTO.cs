using BlazorChatApp.Application.DTOs.ChatRooms;

namespace BlazorChatApp.Application.DTOs.ChatRequests
{
    public class GroupChatRequestDTO : ChatRequestDTO
    {
        public Guid GroupChatRoomId { get; set; }
        public GroupChatRoomDTO GroupChatRoom { get; set; }
    }
}
