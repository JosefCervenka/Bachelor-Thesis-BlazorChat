namespace BlazorChatApp.Application.DTOs.Messages
{
    public class UnreadMessageCountDTO
    {
        public Guid ChatId { get; set; }
        public int MessageCount { get; set; }
        public DateTime TimeStamp { get; set; }
    }

    public class UnreadMessageCountWrapperDTO
    {
        public UnreadMessageCountDTO[] UnreadCounts { get; set; }
    }
}
