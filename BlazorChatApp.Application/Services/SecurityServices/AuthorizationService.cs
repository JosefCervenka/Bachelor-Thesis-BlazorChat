using BlazorChatApp.Application.DTOs.Users;
using BlazorChatApp.Application.Exceptions;
using BlazorChatApp.Domain.Models.Users;
using BlazorChatApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace BlazorChatApp.Application.Services.SecurityServices
{
    public class AuthorizationService
    {
        private readonly string _jwtSecret;

        private readonly string _jwtAudience;

        private readonly string _jwtIssuer;

        private readonly JwtService _jwtService;

        private readonly AppDbContext _context;

        private readonly PasswordService _passwordService;

        private readonly IHttpContextAccessor _httpContextAccessor;

        private static readonly Regex EmailRegex = new Regex(
            @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase
        );

        public AuthorizationService(JwtService jwtService, AppDbContext context, PasswordService passwordService, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _jwtService = jwtService;
            _context = context;
            _passwordService = passwordService;
            _httpContextAccessor = httpContextAccessor;

            _jwtSecret = configuration["jwt:secret"]!;
            _jwtIssuer = configuration["jwt:issuer"]!;
            _jwtAudience = configuration["jwt:audience"]!;
        }

        public async Task<ClaimsPrincipal> GetCredentials()
        {
            var cookie = _httpContextAccessor.HttpContext?.Request.Cookies["jwt"];

            if (string.IsNullOrEmpty(cookie))
                return new ClaimsPrincipal();

            var principal = _jwtService.ValidateJwtToken(
                cookie!,
                _jwtSecret,
                _jwtIssuer,
                _jwtAudience
            );

            if (principal is null)
                return new ClaimsPrincipal();

            return principal;
        }

        public async Task Logout()
        {
            _httpContextAccessor.HttpContext.Response.Cookies.Append("jwt", "", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(-1)
            });
        }

        public async Task Register(UserRegisterDTO userRegisterDTO)
        {

            if (string.IsNullOrWhiteSpace(userRegisterDTO.Email))
                throw new Exception("Email is required.");

            if (!IsValidEmail(userRegisterDTO.Email))
                throw new Exception("Invalid email format.");

            if (string.IsNullOrWhiteSpace(userRegisterDTO.UserName))
                throw new Exception("Username is required.");

            if (userRegisterDTO.UserName.Length < 3)
                throw new Exception("Username must be at least 3 characters long.");

            if (string.IsNullOrWhiteSpace(userRegisterDTO.Password))
                throw new Exception("Password is required.");

            if (userRegisterDTO.Password.Length < 6)
                throw new Exception("Password must be at least 6 characters long.");

            if (userRegisterDTO.Password != userRegisterDTO.PasswordAgain)
                throw new Exception("Passwords do not match.");

            var existingUserName = await _context.Users
                .AnyAsync(x => x.UserName.ToLower() == userRegisterDTO.UserName.ToLower());

            if (existingUserName)
                throw new Exception("Username already exists. Please try another username.");

            var existingEmail = await _context.Users
                .AnyAsync(x => x.Email.ToLower() == userRegisterDTO.Email.ToLower());

            if (existingEmail)
                throw new Exception("Email already exists. Please try another email.");

            var passwordHash = _passwordService.HashPassword(userRegisterDTO.Password, out string salt);

            User user = new User
            {
                Email = userRegisterDTO.Email,
                UserName = userRegisterDTO.UserName,
                PasswordHash = passwordHash,
                Salt = salt,
            };

            _context.Add(user);
            await _context.SaveChangesAsync();

            await Login(new UserLoginDTO
            {
                Email = userRegisterDTO.Email,
                Password = userRegisterDTO.Password
            });
        }

        public async Task Login(UserLoginDTO userLoginDTO)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Email == userLoginDTO.Email);

            if (user is null)
                throw new UserNotFoundException();

            var valid = _passwordService.VerifyPassword(userLoginDTO.Password, user.Salt, user.PasswordHash);

            if (!valid)
                throw new InvalidPasswordException();

            var token = _jwtService.GenerateJwtToken(
                user,
                _jwtSecret,
                _jwtIssuer,
                _jwtAudience
                );

            _httpContextAccessor.HttpContext.Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24)
            });
        }

        public async Task ChangePassword(string currentPassword, string newPassword)
        {
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            if (string.IsNullOrEmpty(userName))
                throw new Exception("User is not authenticated.");
            
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserName == userName);
            if (user is null)
                throw new UserNotFoundException();
            
            var valid = _passwordService.VerifyPassword(currentPassword, user.Salt, user.PasswordHash);
            if (!valid)
                throw new InvalidPasswordException();
            
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new Exception("New password is required.");

            if (newPassword.Length < 6)
                throw new Exception("New password must be at least 6 characters long.");

            var newHashedPassword = _passwordService.HashPassword(newPassword, out string newSalt);
            user.PasswordHash = newHashedPassword;
            user.Salt = newSalt;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                return EmailRegex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }
    }
}
