namespace BlazorChatApp.Domain.Models.Chats
{
    public class GroupChatRoom : ChatRoom
    {
        public byte[] ProfilePicture { get; set; }
        public required string Name { get; set; }
    }
}
