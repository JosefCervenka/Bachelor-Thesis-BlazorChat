using System.ComponentModel.DataAnnotations;

namespace BlazorChatApp.Domain.Models.Base
{
    public class BaseEntity
    {
        [Key]
        public Guid Id { get; set; }
    }
}
