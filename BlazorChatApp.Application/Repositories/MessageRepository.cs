using BlazorChatApp.Application.DTOs.ChatMembers;
using BlazorChatApp.Application.DTOs.ChatRooms;
using BlazorChatApp.Application.DTOs.Messages;
using BlazorChatApp.Application.DTOs.Users;
using BlazorChatApp.Domain.Models.Messages;
using BlazorChatApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Application.Repositories
{
    public class MessageRepository
    {
        private readonly AppDbContext _context;
        public MessageRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UnreadMessageCountDTO[]> UnreadMessageCounts(HttpContext httpContext)
        {
            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == httpContext!.User!.Identity!.Name);

            var result = await _context.Messages
                .Include(x => x.Chat)
                .ThenInclude(x => x.ChatMembers)
                .Where(x => x.Chat.ChatMembers.Any(cm => cm.UserId == currentUser.Id))
                .Where(x => x.CreatedAt > x.Chat.ChatMembers.First(x => x.UserId == currentUser.Id).LastSeen)
                .GroupBy(x => x.ChatId)
                .Select(g => new UnreadMessageCountDTO
                {
                    ChatId = g.Key,
                    MessageCount = g.Count(),
                    TimeStamp = g.Max(x => x.CreatedAt)
                }).ToArrayAsync();

            return result;
        }

        public async Task<MessageDTO> Save(MessageDTO messageDTO)
        {
            var chatMemberId = _context.ChatMembers
                .Where(x => x.User.UserName == messageDTO.ChatMember.User.UserName)
                .Select(x => x.Id)
                .First();

            Message message;

            if (messageDTO is PhotoMessageDTO photoDto)
            {
                message = new PhotoMessage
                {
                    ChatId = messageDTO.ChatRoom.Id,
                    ChatMemberId = chatMemberId,
                    Content = messageDTO.Content ?? string.Empty,
                    CreatedAt = messageDTO.CreatedAt,
                    Photo = photoDto.Photo
                };
            }
            else
            {
                message = new Message
                {
                    ChatId = messageDTO.ChatRoom.Id,
                    ChatMemberId = chatMemberId,
                    Content = messageDTO.Content,
                    CreatedAt = messageDTO.CreatedAt
                };
            }

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            messageDTO.Id = message.Id;
            return messageDTO;
        }

        public async Task<MessageDTO[]> Get(Guid chatRoomId, HttpContext httpContext, int numberOfMessages = 50, int skip = 0)
        {
            var checkIfUserInChat = await _context.ChatMembers
                .AnyAsync(x => x.ChatId == chatRoomId && x.User.UserName == httpContext!.User!.Identity!.Name);


            var messages = await _context.Messages
                .Where(x => x.ChatId == chatRoomId)
                .Include(x => x.ChatMember)
                    .ThenInclude(x => x.User)
                .Include(x => x.Chat)
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(numberOfMessages)
                .Select(x => MapToDTO(x))
                .ToArrayAsync();

            return messages;
        }


        private static MessageDTO MapToDTO(Message message)
        {
            MessageDTO messageDTO;

            if (message is PhotoMessage photoMessage)
            {
                messageDTO = new PhotoMessageDTO
                {
                    Id = photoMessage.Id,
                    Content = photoMessage.Content,
                    CreatedAt = photoMessage.CreatedAt,

                    ChatRoom = new ChatRoomDTO
                    {
                        Id = photoMessage.Chat.Id
                    },
                    ChatMember = new ChatMemberDTO
                    {
                        User = new UserDTO
                        {
                            UserName = photoMessage.ChatMember.User.UserName,
                            FirstName = photoMessage.ChatMember.User.FirstName,
                            Surname = photoMessage.ChatMember.User.Surname
                        }
                    },
                    Photo = photoMessage.Photo


                };
            }

            else
            {
                messageDTO = new MessageDTO
                {
                    Id = message.Id,
                    Content = message.Content,
                    CreatedAt = message.CreatedAt,

                    ChatRoom = new ChatRoomDTO
                    {
                        Id = message.Chat.Id
                    },
                    ChatMember = new ChatMemberDTO
                    {
                        User = new UserDTO
                        {
                            UserName = message.ChatMember.User.UserName,
                            FirstName = message.ChatMember.User.FirstName,
                            Surname = message.ChatMember.User.Surname
                        }
                    },
                };
            }

            return messageDTO;

        }
    }
}
