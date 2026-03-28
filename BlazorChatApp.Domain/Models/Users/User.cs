using BlazorChatApp.Domain.Models.Base;

namespace BlazorChatApp.Domain.Models.Users
{
    public class User : BaseEntity
    {
        public required string Email { get; set; }

        public required string UserName { get; set; }

        public string? FirstName { get; set; }

        public string? Surname { get; set; }

        public required byte[] PasswordHash { get; set; }

        public byte[]? ProfilePhoto { get; set; }

        public required string Salt { get; set; }
    }
}
