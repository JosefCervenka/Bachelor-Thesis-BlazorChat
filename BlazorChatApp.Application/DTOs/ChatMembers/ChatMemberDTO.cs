using BlazorChatApp.Application.DTOs.Users;
using System.Text.Json.Serialization;

namespace BlazorChatApp.Application.DTOs.ChatMembers
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ChatMemberDTO), "ChatMember")]
    [JsonDerivedType(typeof(GroupChatMemberDTO), "GroupChatMember")]
    public class ChatMemberDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public UserDTO User { get; set; }
        public DateTime LastSeen { get; set; }
    }
}
