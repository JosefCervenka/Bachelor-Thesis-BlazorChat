using BlazorChatApp.Application.DTOs.ChatMembers;
using System.Text.Json.Serialization;

namespace BlazorChatApp.Application.DTOs.ChatRooms
{
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(DirectChatRoomDTO), "DirectChatRoom")]
    [JsonDerivedType(typeof(GroupChatRoomDTO), "GroupChatRoom")]
    public class ChatRoomDTO
    {
        public Guid Id { get; set; }
        public ChatMemberDTO[] ChatMembers { get; set; }
    }

    public class ChatRoomWrapperDTO
    {
        public ChatRoomDTO[] ChatRooms { get; set; }
    }
}
