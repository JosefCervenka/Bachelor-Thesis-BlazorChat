namespace BlazorChatApp.Application.DTOs.ChatMembers
{
    public class GroupChatMemberDTO : ChatMemberDTO
    {
        public int ChatMemberRoleId { get; set; }

        public ChatMemberRoleDTO ChatMemberRole { get; set; }
    }
}
