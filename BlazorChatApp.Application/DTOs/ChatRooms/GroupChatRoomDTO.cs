namespace BlazorChatApp.Application.DTOs.ChatRooms
{
    public class GroupChatRoomDTO : ChatRoomDTO
    {
        public byte[] ProfilePicture { get; set; }
        public string Name { get; set; }
    }
}
