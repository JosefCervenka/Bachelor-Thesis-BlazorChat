namespace BlazorChatApp.Application.Exceptions
{
    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message = "User not found") : base(message)
        {
        }
        public UserNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public UserNotFoundException() : base("User not found")
        {
        }
    }
}
