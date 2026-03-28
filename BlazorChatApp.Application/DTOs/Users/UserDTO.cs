namespace BlazorChatApp.Application.DTOs.Users
{
    public class UserDTO
    {
        public Guid Id { get; set; }

        public string? FirstName { get; set; }

        public string? Surname { get; set; }

        public string UserName { get; set; }

        public byte[] ProfilePhoto { get; set; }
    }
}
