using BlazorChatApp.Domain.Models.Base;
using BlazorChatApp.Domain.Models.ChatMembers;
using BlazorChatApp.Domain.Models.Chats;

namespace BlazorChatApp.Domain.Models.Messages
{
    public class Message : BaseEntity
    {
        public required string Content { get; set; }

        public required DateTime CreatedAt { get; set; }

        public required Guid ChatMemberId { get; set; }

        public ChatMember ChatMember { get; set; }

        public required Guid ChatId { get; set; }

        public ChatRoom? Chat { get; set; }
    }
}
