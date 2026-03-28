namespace BlazorChatApp.Application.Exceptions
{
    public class InvalidPasswordException : Exception
    {
        public InvalidPasswordException(string message = "Invalid password") : base(message)
        {
        }
        public InvalidPasswordException(string message, Exception innerException) : base(message, innerException)
        {
        }
        public InvalidPasswordException() : base("Invalid password")
        {
        }
    }
}
