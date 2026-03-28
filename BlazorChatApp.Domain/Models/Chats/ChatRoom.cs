using BlazorChatApp.Domain.Models.Base;
using BlazorChatApp.Domain.Models.ChatMembers;
using BlazorChatApp.Domain.Models.Messages;

namespace BlazorChatApp.Domain.Models.Chats
{
    public class ChatRoom : BaseEntity
    {
        public ICollection<Message> Messages { get; set; }
        public ICollection<ChatMember> ChatMembers { get; set; }
    }
}
