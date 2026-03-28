using BlazorChatApp.Application.DTOs.ChatMembers;
using BlazorChatApp.Application.DTOs.ChatRequests;
using BlazorChatApp.Application.DTOs.ChatRooms;
using BlazorChatApp.Application.DTOs.Users;
using BlazorChatApp.Domain.Models.ChatMembers;
using BlazorChatApp.Domain.Models.ChatRequests;
using BlazorChatApp.Domain.Models.Chats;
using BlazorChatApp.Domain.Models.Messages;
using BlazorChatApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Application.Repositories
{
    public class InvitationRepository
    {
        private readonly AppDbContext _context;

        public InvitationRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRequestDTO?> Delete(Guid id, HttpContext httpContext)
        {
            var chatRequest = await _context.ChatRequests
                .Include(x => x.Initializer)
                .Include(x => x.Invited)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (chatRequest is null)
                return null;

            var requestDTO = new ChatRequestDTO
            {
                Id = chatRequest.Id,
                Initializer = new UserDTO { UserName = chatRequest.Initializer.UserName },
                Invited = new UserDTO { UserName = chatRequest.Invited.UserName }
            };

            _context.Remove(chatRequest);
            await _context.SaveChangesAsync();

            return requestDTO;
        }

        public async Task<(ChatRequestDTO? ChatRequest, string? ErrorMessage)> CreateChatRequest(ChatRequestDTO chatRequestDTO, HttpContext httpContext)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.UserName == httpContext!.User!.Identity!.Name);
            var invited = await _context.Users.FirstOrDefaultAsync(x => x.Id == chatRequestDTO.InvitedId);

            if (currentUser is null || invited is null)
                return (null, "User not found.");

            chatRequestDTO.CreatedAt = DateTime.UtcNow;

            if (chatRequestDTO is FriendRequestDTO)
            {
                var existingRequest = await _context.FriendRequests
                    .AnyAsync(x =>
                        x.ChatRequestStatusId == (int)ChatRequestStatusEnum.Created &&
                        (
                            (x.InitializerId == currentUser.Id && x.InvitedId == invited.Id) ||
                            (x.InitializerId == invited.Id && x.InvitedId == currentUser.Id)
                        )
                    );

                if (existingRequest)
                    return (null, "A friend request is already pending between you and this user.");

                var existingFriendship = await _context.DirectChatRooms
                    .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                    .AnyAsync(x => (x.ChatMembers.Any(x => x.UserId == currentUser.Id) && x.ChatMembers.Any(x => x.UserId == invited.Id)));

                if (existingFriendship)
                    return (null, "You are already friends with this user.");
            }

            if (chatRequestDTO is GroupChatRequestDTO groupChatRequestDTO)
            {
                var existingRequest = await _context.GroupChatRequests
                    .AnyAsync(req =>
                        req.ChatRequestStatusId == (int)ChatRequestStatusEnum.Created &&
                        req.GroupChatRoomId == groupChatRequestDTO.GroupChatRoomId &&
                        req.InvitedId == groupChatRequestDTO.InvitedId
                    );

                if (existingRequest)
                    return (null, "An invitation to this group is already pending for this user.");

                var existingMembership = await _context.GroupChatRooms
                    .AnyAsync(x =>
                        x.Id == groupChatRequestDTO.GroupChatRoomId &&
                        x.ChatMembers.Any(cm => cm.UserId == groupChatRequestDTO.InvitedId)
                    );

                if (existingMembership)
                    return (null, "This user is already a member of the group.");

                var groupChatRoom = await _context.GroupChatRooms
                    .Include(x => x.ChatMembers)
                    .FirstOrDefaultAsync(x => x.Id == groupChatRequestDTO.GroupChatRoomId);

                if (groupChatRoom is null)
                    return (null, "Group chat room not found.");

                var isMember = groupChatRoom.ChatMembers.Any(x => x.UserId == currentUser.Id);
                if (!isMember)
                    return (null, "You are not a member of this group.");
            }

            ChatRequest chatRequest = chatRequestDTO switch
            {
                FriendRequestDTO => new FriendRequest
                {
                    InitializerId = currentUser.Id,
                    InvitedId = invited.Id,
                    CreatedAt = chatRequestDTO.CreatedAt,
                    ChatRequestStatusId = (int)ChatRequestStatusEnum.Created
                },

                GroupChatRequestDTO groupDto => new GroupChatRequest
                {
                    GroupChatRoomId = groupDto.GroupChatRoomId,
                    InitializerId = currentUser.Id,
                    InvitedId = invited.Id,
                    CreatedAt = chatRequestDTO.CreatedAt,
                    ChatRequestStatusId = (int)ChatRequestStatusEnum.Created
                },

                _ => throw new NotImplementedException()
            };

            _context.ChatRequests.Add(chatRequest);
            await _context.SaveChangesAsync();

            chatRequestDTO.Id = chatRequest.Id;
            chatRequestDTO.InitializerId = currentUser.Id;
            chatRequestDTO.ChatRequestStatusId = (int)ChatRequestStatusEnum.Created;

            chatRequestDTO.Initializer = new UserDTO
            {
                Id = currentUser.Id,
                FirstName = currentUser.FirstName,
                Surname = currentUser.Surname,
                UserName = currentUser.UserName,
            };

            chatRequestDTO.Invited = new UserDTO
            {
                Id = invited.Id,
                FirstName = invited.FirstName,
                Surname = invited.Surname,
                UserName = invited.UserName,
            };

            chatRequestDTO.ChatRequestStatus = new ChatRequestStatusDTO
            {
                Id = chatRequestDTO.ChatRequestStatusId,
                StatusCode = nameof(ChatRequestStatusEnum.Created)
            };

            if (chatRequestDTO is GroupChatRequestDTO groupRequestDTO)
            {
                var group = await _context.GroupChatRooms
                    .FirstAsync(x => x.Id == groupRequestDTO.GroupChatRoomId);

                groupRequestDTO.GroupChatRoom = new GroupChatRoomDTO
                {
                    Id = group.Id,
                    Name = group.Name
                };
            }

            return (chatRequestDTO, null);
        }

        public async Task<ChatRequestDTO[]> GetInitializedChatRequests(HttpContext httpContext)
        {
            var userName = httpContext?.User?.Identity?.Name;

            ChatRequestDTO[] friendRequestDTO = await _context.FriendRequests
                .Include(x => x.Initializer)
                .Include(x => x.Invited)
                .Include(x => x.ChatRequestStatus)
                .Where(x => x.Initializer.UserName == userName)
                .Where(x => x.ChatRequestStatusId == (int)ChatRequestStatusEnum.Created)
                .AsNoTracking()
                .Select(x => new FriendRequestDTO
                {
                    Id = x.Id,
                    Invited = new UserDTO
                    {
                        UserName = x.Invited.UserName,
                    },
                    Initializer = new UserDTO
                    {
                        UserName = x.Initializer.UserName,
                    },
                    ChatRequestStatus = new ChatRequestStatusDTO
                    {
                        Id = x.ChatRequestStatusId,
                    },
                    CreatedAt = x.CreatedAt,
                })
                .ToArrayAsync();

            ChatRequestDTO[] groupRequestDTO = await _context.GroupChatRequests
                .Include(x => x.Initializer)
                .Include(x => x.Invited)
                .Include(x => x.ChatRequestStatus)
                .Where(x => x.Initializer.UserName == userName)
                .Where(x => x.ChatRequestStatusId == (int)ChatRequestStatusEnum.Created)
                .Include(x => x.GroupChatRoom)
                .AsNoTracking()
                .Select(x => new GroupChatRequestDTO
                {
                    Id = x.Id,
                    Invited = new UserDTO
                    {
                        UserName = x.Invited.UserName,
                    },
                    Initializer = new UserDTO
                    {
                        UserName = x.Initializer.UserName,
                    },
                    ChatRequestStatus = new ChatRequestStatusDTO
                    {
                        Id = x.ChatRequestStatusId,
                    },
                    CreatedAt = x.CreatedAt,
                    GroupChatRoom = new GroupChatRoomDTO
                    {
                        Name = x.GroupChatRoom.Name
                    }
                })
                .ToArrayAsync();

            return friendRequestDTO.Concat(groupRequestDTO).ToArray();
        }

        public async Task<ChatRequestDTO[]> GetInvitedChatRequests(HttpContext httpContext)
        {
            var userName = httpContext?.User?.Identity?.Name;

            ChatRequestDTO[] friendRequestDTO = await _context.FriendRequests
                .Include(x => x.Initializer)
                .Include(x => x.Invited)
                .Include(x => x.ChatRequestStatus)
                .Where(x => x.Invited.UserName == userName)
                .Where(x => x.ChatRequestStatusId == (int)ChatRequestStatusEnum.Created)
                .AsNoTracking()
                .Select(x => new FriendRequestDTO
                {
                    Id = x.Id,
                    Invited = new UserDTO
                    {
                        UserName = x.Invited.UserName,
                    },
                    Initializer = new UserDTO
                    {
                        UserName = x.Initializer.UserName,
                    },
                    ChatRequestStatus = new ChatRequestStatusDTO
                    {
                        Id = x.ChatRequestStatusId,
                    },
                    CreatedAt = x.CreatedAt,
                })
                .ToArrayAsync();

            ChatRequestDTO[] groupRequestDTO = await _context.GroupChatRequests
                .Include(x => x.Initializer)
                .Include(x => x.Invited)
                .Include(x => x.ChatRequestStatus)
                .Where(x => x.Invited.UserName == userName)
                .Where(x => x.ChatRequestStatusId == (int)ChatRequestStatusEnum.Created)
                .Include(x => x.GroupChatRoom)
                .AsNoTracking()
                .Select(x => new GroupChatRequestDTO
                {
                    Id = x.Id,
                    Invited = new UserDTO
                    {
                        UserName = x.Invited.UserName,
                    },
                    Initializer = new UserDTO
                    {
                        UserName = x.Initializer.UserName,
                    },
                    ChatRequestStatus = new ChatRequestStatusDTO
                    {
                        Id = x.ChatRequestStatusId,
                    },
                    CreatedAt = x.CreatedAt,
                    GroupChatRoom = new GroupChatRoomDTO
                    {
                        Name = x.GroupChatRoom.Name
                    }
                })
                .ToArrayAsync();

            return friendRequestDTO.Concat(groupRequestDTO).ToArray();
        }

        public async Task<(ChatRequestDTO?, DirectChatRoomDTO?, GroupChatRoomDTO?)> ResponseInvitation(ChatRequestDTO chatRequestDTO, HttpContext httpContext)
        {
            var userName = httpContext?.User?.Identity?.Name;

            var chatRequest = await _context.ChatRequests
                .Include(x => x.Initializer)
                .Include(x => x.Invited)
                .FirstOrDefaultAsync(x => x.Id == chatRequestDTO.Id);

            if (chatRequest is null || chatRequest.ChatRequestStatusId != (int)ChatRequestStatusEnum.Created)
                return (null, null, null);

            if (userName != chatRequest.Invited.UserName)
                return (null, null, null);

            chatRequest.ChatRequestStatusId = chatRequestDTO.ChatRequestStatusId;

            DirectChatRoomDTO? directChatDTO = null;
            GroupChatRoomDTO? groupChatDTO = null;

            if (chatRequest is GroupChatRequest chatGroupRequestDTO)
            {
                var groupChat = await _context.GroupChatRooms.FirstOrDefaultAsync(x => x.Id == chatGroupRequestDTO.GroupChatRoomId);

                if (groupChat is not null)
                {
                    groupChatDTO = new GroupChatRoomDTO
                    {
                        Id = groupChat.Id,
                        Name = groupChat.Name,
                        ProfilePicture = groupChat.ProfilePicture
                    };

                    _context.ChatMembers.Add(new GroupChatMember
                    {
                        ChatId = groupChat.Id,
                        UserId = chatRequest.InvitedId,
                        ChatMemberRoleId = (int)ChatMemberRoleEnum.User,
                        LastSeen = DateTime.UtcNow,
                    });
                }
            }

            if (chatRequestDTO is FriendRequestDTO && chatRequestDTO.ChatRequestStatusId == (int)ChatRequestStatusEnum.Accepted)
            {
                var directChat = new DirectChatRoom
                {
                    Messages = new List<Message>()
                };

                var chatMember1 = new ChatMember()
                {
                    UserId = chatRequest.InvitedId,
                    Chat = directChat,
                    LastSeen = DateTime.UtcNow,
                };

                var chatMember2 = new ChatMember()
                {
                    UserId = chatRequest.InitializerId,
                    Chat = directChat,
                    LastSeen = DateTime.UtcNow,
                };

                _context.DirectChatRooms.Add(directChat);
                _context.ChatMembers.Add(chatMember1);
                _context.ChatMembers.Add(chatMember2);

                directChatDTO = new DirectChatRoomDTO
                {

                    Id = directChat.Id,
                    ChatMembers = [
                        new ChatMemberDTO {
                            User = new UserDTO{
                                Id = chatRequest.Initializer.Id,
                                FirstName = chatRequest.Initializer.FirstName,
                                Surname = chatRequest.Initializer.Surname,
                                UserName = chatRequest.Initializer.UserName,
                                ProfilePhoto = chatRequest.Initializer.ProfilePhoto
                            },
                            LastSeen = chatMember2.LastSeen
                        },
                        new ChatMemberDTO {
                            User = new UserDTO{
                                Id = chatRequest.Invited.Id,
                                FirstName = chatRequest.Invited.FirstName,
                                Surname = chatRequest.Invited.Surname,
                                UserName = chatRequest.Invited.UserName,
                                ProfilePhoto = chatRequest.Invited.ProfilePhoto
                            },
                            LastSeen = chatMember1.LastSeen
                        },
                    ]
                };
            }

            await _context.SaveChangesAsync();

            return (chatRequestDTO, directChatDTO, groupChatDTO);
        }
    }
}