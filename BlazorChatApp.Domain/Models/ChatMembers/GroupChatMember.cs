namespace BlazorChatApp.Domain.Models.ChatMembers
{
    public class GroupChatMember : ChatMember
    {
        public int ChatMemberRoleId { get; set; }
        public ChatMemberRole ChatMemberRole { get; set; }
    }
}
