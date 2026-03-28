using BlazorChatApp.Domain.Models.Base;
using BlazorChatApp.Domain.Models.Users;

namespace BlazorChatApp.Domain.Models.ChatRequests
{
    public class ChatRequest : BaseEntity
    {
        public Guid InitializerId { get; set; }
        public User Initializer { get; set; }
        public Guid InvitedId { get; set; }
        public User Invited { get; set; }
        public int ChatRequestStatusId { get; set; }
        public ChatRequestStatus ChatRequestStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
