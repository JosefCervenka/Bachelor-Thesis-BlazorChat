using BlazorChatApp.Infrastructure;
using BlazorChatApp.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Hubs
{
    [Authorize]
    public class ChatRoomHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly AppDbContext _context;

        private readonly MessageRepository _messageRepository;

        public ChatRoomHub(IHttpContextAccessor httpContextAccessor, AppDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _messageRepository = new MessageRepository(_context);
        }

        public async Task LastVisited(Guid chatRoomId)
        {
            var userName = _httpContextAccessor.HttpContext?.User.Identity?.Name;

            var member = _context.ChatMembers
                .Include(x => x.User)
                .FirstOrDefault(x => x.User.UserName == userName && x.ChatId == chatRoomId);

            if (member is null)
                return;

            member.LastSeen = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
