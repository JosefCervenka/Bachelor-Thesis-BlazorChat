using BlazorChatApp.Application.DTOs.Users;
using System.Text.Json.Serialization;

namespace BlazorChatApp.Application.DTOs.ChatRequests
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(GroupChatRequestDTO), "GroupRequest")]
    [JsonDerivedType(typeof(FriendRequestDTO), "FriendRequest")]
    public class ChatRequestDTO
    {
        public Guid Id { get; set; }
        public Guid InitializerId { get; set; }
        public UserDTO Initializer { get; set; }
        public Guid InvitedId { get; set; }
        public UserDTO Invited { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int ChatRequestStatusId { get; set; }
        public ChatRequestStatusDTO ChatRequestStatus { get; set; }
    }

    public class ChatRequestWrapperDTO
    {
        public ChatRequestDTO[] ChatRequests { get; set; }
    }
}
