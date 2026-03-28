using BlazorChatApp.Application.DTOs.Messages;
using BlazorChatApp.Infrastructure;
using BlazorChatApp.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BlazorChatApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly MessageRepository _messageService;
        public MessageController(AppDbContext context)
        {
            _context = context;
            _messageService = new MessageRepository(_context);
        }

        [HttpGet("{chatRoomId}")]
        public async Task<IActionResult> GetMessages([FromRoute] Guid chatRoomId, [FromQuery] int numberOfMessage = 50, [FromQuery] int skip = 0)
        {
            return Ok(new MessageWrapperDTO
            {
                Messages = (await _messageService.Get(chatRoomId, HttpContext, numberOfMessage, skip)).ToArray()
            });
        }

        [HttpGet("unread-messages-count")]
        public async Task<IActionResult> GetUnreadMessagesCount()
        {
            var result = await _messageService.UnreadMessageCounts(HttpContext);

            return Ok(new UnreadMessageCountWrapperDTO
            {
                UnreadCounts = result
            });
        }
    }
}
