using System.ComponentModel.DataAnnotations;

namespace BlazorChatApp.Application.DTOs.Users
{
    public class UserLoginDTO
    {
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }


        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }
    }
}
