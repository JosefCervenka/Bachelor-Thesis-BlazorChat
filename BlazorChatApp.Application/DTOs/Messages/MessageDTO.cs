using BlazorChatApp.Application.DTOs.ChatMembers;
using BlazorChatApp.Application.DTOs.ChatRooms;
using System.Text.Json.Serialization;

namespace BlazorChatApp.Application.DTOs.Messages
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(PhotoMessageDTO), "PhotoMessage")]
    public class MessageDTO
    {
        public Guid Id { get; set; }
        public required string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public Guid ChatMemberId { get; set; }
        public ChatMemberDTO ChatMember { get; set; }
        public Guid ChatRoomId { get; set; }
        public ChatRoomDTO ChatRoom { get; set; }
    }

    public class MessageWrapperDTO
    {
        public MessageDTO[] Messages { get; set; }
    }
}
