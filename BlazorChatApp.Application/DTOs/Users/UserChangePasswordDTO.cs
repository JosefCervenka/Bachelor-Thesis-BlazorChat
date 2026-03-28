namespace BlazorChatApp.Application.DTOs.Users
{
    public class UserChangePasswordDTO
    {
        public string CurrentPassword { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmNewPassword { get; set; }
    }
}
