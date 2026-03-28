using BlazorChatApp.Domain.Models.Base;
using BlazorChatApp.Domain.Models.Chats;
using BlazorChatApp.Domain.Models.Users;

namespace BlazorChatApp.Domain.Models.ChatMembers
{
    public class ChatMember : BaseEntity
    {
        public Guid ChatId { get; set; }

        public ChatRoom Chat { get; set; }

        public Guid UserId { get; set; }

        public User User { get; set; }

        public DateTime LastSeen { get; set; } = DateTime.Now;
    }
}
