using BlazorChatApp.Application.DTOs.ChatMembers;
using BlazorChatApp.Application.DTOs.Messages;
using BlazorChatApp.Application.DTOs.Users;
using BlazorChatApp.Infrastructure;
using BlazorChatApp.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly AppDbContext _context;

        private readonly MessageRepository _messageRepository;

        private readonly IHubContext<ChatRoomHub> _chatRoomHubContext;

        public ChatHub(IHttpContextAccessor httpContextAccessor, AppDbContext context, IHubContext<ChatRoomHub> chatRoomHubContext)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _chatRoomHubContext = chatRoomHubContext;
            _messageRepository = new MessageRepository(_context);
        }

        public override async Task OnConnectedAsync()
        {
            var httpContext = Context.GetHttpContext();
            var chatId = httpContext?.Request.Query["chatRoomId"].ToString();

            if (!string.IsNullOrEmpty(chatId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
            }

            await base.OnConnectedAsync();
        }

        public async Task SendMessage(MessageDTO message)
        {
            var user = _httpContextAccessor.HttpContext?.User;

            message.ChatMember = new ChatMemberDTO
            {
                User = new UserDTO
                {
                    UserName = user?.Identity?.Name ?? string.Empty
                }
            };

            message.CreatedAt = DateTime.UtcNow;

            var userNames = await _context.ChatRooms
                .Where(c => c.Id == message.ChatRoom.Id)
                .SelectMany(c => c.ChatMembers.Select(cm => cm.User.UserName))
                .ToListAsync();

            if (!userNames.Contains(message.ChatMember.User.UserName))
                return;

            await _messageRepository.Save(message);


            if (message is PhotoMessageDTO)
                await Clients.Group(message.ChatRoom!.Id.ToString()).SendAsync("ReceiveMessage", (PhotoMessageDTO)message);

            else
                await Clients.Group(message.ChatRoom!.Id.ToString()).SendAsync("ReceiveMessage", message);

            foreach (var userName in userNames)
            {
                if (userName != message.ChatMember.User.UserName)
                {
                    await _chatRoomHubContext.Clients.User(userName).SendAsync("MessageNotification", message.ChatRoomId, message.CreatedAt);
                }
            }
        }
    }
}
