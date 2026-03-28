using System.ComponentModel.DataAnnotations;

namespace BlazorChatApp.Domain.Models.ChatRequests
{
    public class ChatRequestStatus
    {
        [Key]
        public int Id { get; set; }

        public string StatusCode { get; set; }
    }

    public enum ChatRequestStatusEnum
    {
        Created = 1,

        Accepted = 2,

        Decline = 3,
    }
}
