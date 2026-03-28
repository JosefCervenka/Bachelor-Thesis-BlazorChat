using BlazorChatApp.Application.DTOs.Users;
using BlazorChatApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Application.Repositories
{
    public class UserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserDTO[]> SearchByUserName(string search, HttpContext httpContext)
        {
            var currentUserName = httpContext!.User!.Identity!.Name;

            var notInvitedUsers = await _context.Users
                .Where(x => x.UserName != currentUserName)
                .Where(x => EF.Functions.ILike(x.UserName, $"%{search}%"))
                .Take(5)
                .Select(x => new UserDTO
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    Surname = x.Surname,
                    UserName = x.UserName,
                })
                .AsNoTracking()
                .ToArrayAsync();

            return notInvitedUsers;
        }

        public async Task<UserDTO?> GetById(Guid id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Select(x => new UserDTO
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    Surname = x.Surname,
                    UserName = x.UserName
                })
                .FirstOrDefaultAsync(x => x.Id == id);
            return user;
        }

        public async Task<UserDTO?> GetByUserName(string userName)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Select(x => new UserDTO
                {
                    Id = x.Id,
                    FirstName = x.FirstName,
                    Surname = x.Surname,
                    UserName = x.UserName,
                    ProfilePhoto = x.ProfilePhoto
                })
                .FirstOrDefaultAsync(x => EF.Functions.ILike(x.UserName, $"%{userName}%"));
            return user;
        }

        public async Task UpdateUserProfile(UserDTO userUpdateDTO, HttpContext httpContext)
        {
            var currentUserName = httpContext!.User!.Identity!.Name;
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == currentUserName);
            if (user is null)
                return;
            user.FirstName = userUpdateDTO.FirstName;
            user.Surname = userUpdateDTO.Surname;

            if (userUpdateDTO.ProfilePhoto is not null)
            {
                user.ProfilePhoto = userUpdateDTO.ProfilePhoto;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }
    }
}
