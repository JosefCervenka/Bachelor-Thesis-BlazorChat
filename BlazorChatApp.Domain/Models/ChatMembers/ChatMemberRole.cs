using System.ComponentModel.DataAnnotations;

namespace BlazorChatApp.Domain.Models.ChatMembers
{
    public class ChatMemberRole
    {
        [Key]
        public int Id { get; set; }

        public string Name { get; set; }
    }
}

public enum ChatMemberRoleEnum
{
    User = 1,

    Owner = 2,

    Admin = 3,
}