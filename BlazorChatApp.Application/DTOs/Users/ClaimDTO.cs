namespace BlazorChatApp.Application.DTOs.Users
{
    public class ClaimsWrapperDTO
    {
        public ClaimDTO[] Claims { get; set; }
    }

    public class ClaimDTO
    {
        public string Type { get; set; }
        public string Value { get; set; }
    }
}
