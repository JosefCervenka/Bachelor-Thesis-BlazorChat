using System.ComponentModel.DataAnnotations;

namespace BlazorChatApp.Application.DTOs.Users
{
    public class UserRegisterDTO : IValidatableObject
    {
        [Required(ErrorMessage = "Username is required.")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters.")]
        [MaxLength(40, ErrorMessage = "Username must be at most 40 characters.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Password confirmation is required.")]
        public string PasswordAgain { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Password != PasswordAgain)
            {
                yield return new ValidationResult(
                    "Passwords do not match.",
                    new[] { nameof(PasswordAgain) });
            }
        }
    }
}