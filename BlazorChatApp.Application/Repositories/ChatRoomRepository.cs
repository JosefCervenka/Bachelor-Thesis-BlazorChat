using BlazorChatApp.Application.DTOs.ChatMembers;
using BlazorChatApp.Application.DTOs.ChatRequests;
using BlazorChatApp.Application.DTOs.ChatRooms;
using BlazorChatApp.Application.DTOs.Users;
using BlazorChatApp.Domain.Models.ChatMembers;
using BlazorChatApp.Domain.Models.Chats;
using BlazorChatApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Application.Repositories
{
    public class ChatRoomRepository
    {
        private readonly AppDbContext _context;
        public ChatRoomRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<GroupChatRoomDTO> Save(GroupChatRoomDTO groupChatRoomDTO, HttpContext httpContext)
        {
            var currentUser = await _context.Users.FirstOrDefaultAsync(x => x.UserName == httpContext.User.Identity!.Name);

            var groupChatRoom = new GroupChatRoom
            {
                Name = groupChatRoomDTO.Name,
                ChatMembers = [

                    new GroupChatMember
                    {
                        UserId = currentUser.Id,
                        User = currentUser,
                        ChatMemberRoleId = (int)ChatMemberRoleEnum.Owner,
                        LastSeen = DateTime.UtcNow
                    },
                ]
            };
            _context.GroupChatRooms.Add(groupChatRoom);
            await _context.SaveChangesAsync();

            groupChatRoomDTO.Id = groupChatRoom.Id;
            return groupChatRoomDTO;
        }

        public async Task<(bool Success, string ErrorMessage, ChatMemberDTO RemovedMember)> RemoveUserFromChat(Guid chatId, Guid chatMemberId, HttpContext httpContext)
        {
            if (!_context.ChatRooms.Any(x => x.Id == chatId && x is GroupChatRoom))
                return (false, "Chat room not found or is not a group chat", null);

            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == httpContext!.User!.Identity!.Name);

            var chatMember = await _context.GroupChatMembers
                .Include(x => x.User)
                .Include(x => x.ChatMemberRole)
                .FirstOrDefaultAsync(x => x.Id == chatMemberId && x.ChatId == chatId);

            if (chatMember is null)
                return (false, "Chat member not found", null);

            bool isRemovingSelf = chatMember.UserId == currentUser.Id;

            if (!isRemovingSelf)
            {
                var currentUserRole = await _context.GroupChatMembers
                    .FirstOrDefaultAsync(x => x.ChatId == chatId && x.UserId == currentUser.Id);

                if (currentUserRole is null || (currentUserRole.ChatMemberRoleId != (int)ChatMemberRoleEnum.Owner && currentUserRole.ChatMemberRoleId != (int)ChatMemberRoleEnum.Admin))
                {
                    return (false, "You don't have permission to remove members", null);
                }
            }

            if (chatMember.ChatMemberRoleId == (int)ChatMemberRoleEnum.Owner)
                return (false, "Cannot remove the owner.", null);

            _context.ChatMembers.Remove(chatMember);
            await _context.SaveChangesAsync();

            var removedMemberDto = MapToDTO(chatMember);

            return (true, null, removedMemberDto);
        }

        public async Task<(bool Success, string ErrorMessage, ChatRoom ChatRoom, List<ChatRequestDTO> GroupChatRequests)> DeleteChatRoom(Guid chatId, HttpContext httpContext)
        {
            var chatRoom = await _context.ChatRooms
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => ((GroupChatMember)x).ChatMemberRole)
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (chatRoom is null)
                return (false, "Chat room not found", null, null);

            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == httpContext!.User!.Identity!.Name);

            if (chatRoom is GroupChatRoom)
            {
                var ownerRole = chatRoom.ChatMembers.Any(x =>
                    x.UserId == currentUser!.Id &&
                    x is GroupChatMember member &&
                    member.ChatMemberRoleId == (int)ChatMemberRoleEnum.Owner);

                if (!ownerRole)
                    return (false, "Forbidden: Only owner can delete the chat room", null, null);
            }

            var groupChatRequestsEntities = await _context.GroupChatRequests
                .Include(x => x.Invited)
                .Where(x => x.GroupChatRoomId == chatId)
                .ToListAsync();

            var groupChatRequests = groupChatRequestsEntities.Select(x => new ChatRequestDTO
            {
                Id = x.Id,
                Invited = new UserDTO
                {
                    Id = x.Invited.Id,
                    UserName = x.Invited.UserName
                }
            }).ToList();

            _context.ChatRooms.Remove(chatRoom);
            await _context.SaveChangesAsync();

            return (true, null, chatRoom, groupChatRequests);
        }

        public async Task<ChatRoomDTO[]> GetWhereMember(HttpContext httpContext)
        {
            var userName = httpContext!.User.Identity?.Name;

            var user = _context.Users.FirstOrDefault(x => x.UserName == userName);

            ChatRoomDTO[] groupChatRooms = await _context.GroupChatRooms
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => (x.Chat as GroupChatRoom)!.ChatMembers)
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                .Where(x => x.ChatMembers.Any(x => x.UserId == user.Id))
                .Select(x => new GroupChatRoomDTO
                {
                    Id = x.Id,
                    Name = x.Name,
                    ProfilePicture = x.ProfilePicture,
                    ChatMembers = x.ChatMembers.Select(x => new GroupChatMemberDTO
                    {
                        User = new UserDTO
                        {
                            Id = x.User.Id,
                            UserName = x.User.UserName,
                            FirstName = x.User.FirstName,
                            Surname = x.User.Surname,
                            ProfilePhoto = x.User.ProfilePhoto
                        },
                        LastSeen = x.LastSeen
                    }).ToArray()
                })
                .ToArrayAsync();

            ChatRoomDTO[] directChatRooms = await _context.DirectChatRooms
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                .Where(x => x.ChatMembers.Any(x => x.UserId == user.Id))
                .Select(x => new DirectChatRoomDTO
                {
                    Id = x.Id,
                    ChatMembers = x.ChatMembers.Select(x => new ChatMemberDTO
                    {
                        User = new UserDTO
                        {
                            Id = x.User.Id,
                            UserName = x.User.UserName,
                            FirstName = x.User.FirstName,
                            Surname = x.User.Surname,
                            ProfilePhoto = x.User.ProfilePhoto
                        },
                        LastSeen = x.LastSeen
                    }).ToArray()
                })
                .ToArrayAsync();

            return groupChatRooms
                .Concat(directChatRooms)
                .ToArray();
        }

        public async Task<(bool Success, string ErrorMessage, ChatMemberDTO[] ChatMembers)> GetChatMembers(Guid chatRoomId, HttpContext httpContext)
        {
            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == httpContext!.User!.Identity!.Name);

            var isMember = await _context.ChatMembers
                .AnyAsync(x => x.ChatId == chatRoomId && x.UserId == currentUser.Id);

            if (!isMember)
                return (false, "Forbidden: You are not a member of this chat room", null);

            ChatMemberDTO[] chatMembers = await _context.ChatMembers.Where(x => x.ChatId == chatRoomId)
                .Include(x => x.User)
                .Include(x => x.Chat)
                    .Include(x => (x as GroupChatMember)!.ChatMemberRole)
                .Select(x => MapToDTO(x))
                .ToArrayAsync();

            return (true, null, chatMembers);
        }

        private static ChatMemberDTO MapToDTO(ChatMember chatMember)
        {
            if (chatMember is GroupChatMember groupChatMember)
            {
                return new GroupChatMemberDTO
                {
                    Id = chatMember.Id,
                    UserId = chatMember.UserId,
                    ChatMemberRoleId = groupChatMember.ChatMemberRoleId,
                    ChatMemberRole = new ChatMemberRoleDTO
                    {
                        Id = groupChatMember.ChatMemberRole.Id,
                        Name = groupChatMember.ChatMemberRole.Name,
                    },
                    User = new UserDTO
                    {
                        Id = chatMember.User.Id,
                        UserName = chatMember.User.UserName,
                    }
                };
            }

            return new ChatMemberDTO
            {
                Id = chatMember.Id,
                UserId = chatMember.UserId,
                User = new UserDTO
                {
                    Id = chatMember.User.Id,
                    UserName = chatMember.User.UserName,
                }
            };
        }

        public async Task<(bool Success, string ErrorMessage, ChatMemberDTO MemberShip)> GetUserMemberShip(Guid chatRoomId, HttpContext httpContext)
        {
            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == httpContext!.User!.Identity!.Name);

            if (currentUser is null)
                return (false, "User not found", null);

            var chatMember = await _context.ChatMembers
                .Include(x => x.User)
                .Include(x => x.Chat)
                .Include(x => (x as GroupChatMember)!.ChatMemberRole)
                .FirstOrDefaultAsync(x => x.ChatId == chatRoomId && x.UserId == currentUser.Id);

            if (chatMember is null)
                return (false, "User is not a member of this chat room", null);

            var memberDto = MapToDTO(chatMember);

            return (true, null, memberDto);
        }

        public async Task<(bool Success, string ErrorMessage, ChatMemberDTO UpdatedMember)> ChangeUserRole(Guid chatId, Guid chatMemberId, int newRoleId, HttpContext httpContext)
        {
            if (!_context.ChatRooms.Any(x => x.Id == chatId && x is GroupChatRoom))
                return (false, "Chat room not found or is not a group chat", null);

            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == httpContext!.User!.Identity!.Name);

            var currentUserRole = await _context.GroupChatMembers
                .FirstOrDefaultAsync(x => x.ChatId == chatId && x.UserId == currentUser.Id);

            if (currentUserRole is null || currentUserRole.ChatMemberRoleId != (int)ChatMemberRoleEnum.Owner)
            {
                return (false, "Forbidden: Only the owner can change member roles", null);
            }

            var targetMember = await _context.GroupChatMembers
                .Include(x => x.User)
                .Include(x => x.ChatMemberRole)
                .FirstOrDefaultAsync(x => x.Id == chatMemberId && x.ChatId == chatId);

            if (targetMember is null)
                return (false, "Chat member not found", null);

            if (targetMember.UserId == currentUser.Id)
                return (false, "Owner cannot change their own role", null);

            if (targetMember.ChatMemberRoleId == (int)ChatMemberRoleEnum.Owner)
                return (false, "Cannot change the role of another owner", null);

            if (newRoleId != (int)ChatMemberRoleEnum.Admin && newRoleId != (int)ChatMemberRoleEnum.User)
                return (false, "Invalid role. Can only set Admin or Member roles", null);

            targetMember.ChatMemberRoleId = newRoleId;
            await _context.SaveChangesAsync();

            await _context.Entry(targetMember).Reference(x => x.ChatMemberRole).LoadAsync();

            var updatedMemberDto = MapToDTO(targetMember);

            return (true, null, updatedMemberDto);
        }

        public async Task<(bool Success, string? ErrorMessage, GroupChatRoomDTO? groupChatRoomDTO)> Update(GroupChatRoomDTO groupChatRoomDTO, HttpContext httpContext)
        {
            if (!_context.ChatRooms.Any(x => x.Id == groupChatRoomDTO.Id && x is GroupChatRoom))
                return (false, "not found", null);

            var currentMember = await _context.ChatMembers
                .Include(x => x.User)
                .Include(x => (x as GroupChatMember)!.ChatMemberRole)
                .FirstOrDefaultAsync(x => x.ChatId == groupChatRoomDTO.Id && x.User.UserName == httpContext.User.Identity!.Name);

            if (currentMember is null || !(
               ((currentMember as GroupChatMember)!.ChatMemberRoleId != (int)ChatMemberRoleEnum.Owner) ||
               ((currentMember as GroupChatMember)!.ChatMemberRoleId != (int)ChatMemberRoleEnum.Admin)))
            {
                return (false, "Forbidden", null);
            }

            var groupChat = _context.GroupChatRooms
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                .FirstOrDefault(x => x.Id == groupChatRoomDTO.Id);

            if (groupChatRoomDTO?.Name is not null && groupChatRoomDTO.Name != string.Empty)
                groupChat!.Name = groupChatRoomDTO.Name ?? groupChat!.Name;

            if (groupChatRoomDTO?.ProfilePicture is not null)
                groupChat?.ProfilePicture = groupChatRoomDTO.ProfilePicture;

            await _context.SaveChangesAsync();
            return (true, null, new GroupChatRoomDTO
            {
                Id = groupChat!.Id,
                Name = groupChat.Name,
                ProfilePicture = groupChat.ProfilePicture,

                ChatMembers = groupChat.ChatMembers.Select(x => new GroupChatMemberDTO
                {
                    Id = x.Id,
                    UserId = x.UserId,
                    User = new UserDTO
                    {
                        Id = x.User.Id,
                        UserName = x.User.UserName
                    },
                    LastSeen = x.LastSeen
                }).ToArray()
            });
        }

        public async Task<(bool Success, string ErrorMessage, ChatRoomDTO ChatRoom)> GetChatRoom(Guid chatId, HttpContext httpContext)
        {
            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == httpContext!.User!.Identity!.Name);

            if (currentUser is null)
                return (false, "User not found", null);

            var isMember = await _context.ChatMembers
                .AnyAsync(x => x.ChatId == chatId && x.UserId == currentUser.Id);

            if (!isMember)
                return (false, "Forbidden: You are not a member of this chat room", null);

            var groupChatRoom = await _context.GroupChatRooms
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => ((GroupChatMember)x).ChatMemberRole)
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (groupChatRoom is not null)
            {
                var groupChatDto = new GroupChatRoomDTO
                {
                    Id = groupChatRoom.Id,
                    Name = groupChatRoom.Name,
                    ProfilePicture = groupChatRoom.ProfilePicture,
                    ChatMembers = groupChatRoom.ChatMembers.Select(x => new GroupChatMemberDTO
                    {
                        Id = x.Id,
                        UserId = x.UserId,
                        ChatMemberRoleId = ((GroupChatMember)x).ChatMemberRoleId,
                        ChatMemberRole = new ChatMemberRoleDTO
                        {
                            Id = ((GroupChatMember)x).ChatMemberRole.Id,
                            Name = ((GroupChatMember)x).ChatMemberRole.Name,
                        },
                        User = new UserDTO
                        {
                            Id = x.User.Id,
                            UserName = x.User.UserName,
                            FirstName = x.User.FirstName,
                            Surname = x.User.Surname,
                            ProfilePhoto = x.User.ProfilePhoto
                        },
                        LastSeen = x.LastSeen
                    }).ToArray()
                };
                return (true, null, groupChatDto);
            }

            var directChatRoom = await _context.DirectChatRooms
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == chatId);

            if (directChatRoom is not null)
            {
                var directChatDto = new DirectChatRoomDTO
                {
                    Id = directChatRoom.Id,
                    ChatMembers = directChatRoom.ChatMembers.Select(x => new ChatMemberDTO
                    {
                        Id = x.Id,
                        UserId = x.UserId,
                        User = new UserDTO
                        {
                            Id = x.User.Id,
                            UserName = x.User.UserName,
                            FirstName = x.User.FirstName,
                            Surname = x.User.Surname,
                            ProfilePhoto = x.User.ProfilePhoto
                        },
                        LastSeen = x.LastSeen
                    }).ToArray()
                };
                return (true, null, directChatDto);
            }

            return (false, "Chat room not found", null);
        }
    }
}
