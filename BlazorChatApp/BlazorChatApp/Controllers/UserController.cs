using BlazorChatApp.Application.DTOs.Users;
using BlazorChatApp.Application.Services.SecurityServices;
using BlazorChatApp.Hubs;
using BlazorChatApp.Infrastructure;
using BlazorChatApp.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorChatApp.Controllers
{
    [Route("api/[controller]")]
    public class UserController : Controller
    {
        private readonly AppDbContext _context;

        private readonly AuthorizationService _authorizationService;

        private readonly UserRepository _userRepository;

        private readonly ChatRoomRepository _chatRoomRepository;

        private readonly IHubContext<ChatRoomHub> _chatHubContext;

        public UserController(AppDbContext context, AuthorizationService authorizationService, IHubContext<ChatRoomHub> chatHubContext)
        {
            _context = context;
            _authorizationService = authorizationService;
            _chatHubContext = chatHubContext;
            _userRepository = new UserRepository(_context);
            _chatRoomRepository = new ChatRoomRepository(_context);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromForm] UserRegisterDTO userRegisterDTO)
        {
            try
            {
                await _authorizationService.Register(userRegisterDTO);
                return Ok();

            }
            catch (Exception ex)
            {

                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _authorizationService.Logout();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromForm] UserLoginDTO userLoginDTO)
        {
            try
            {
                await _authorizationService.Login(userLoginDTO);

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("auth")]
        public async Task<IActionResult> GetCredentials()
        {
            try
            {
                var userInfo = await _authorizationService.GetCredentials();

                var claims = userInfo.Claims
                    .Select(x => new ClaimDTO { Type = x.Type, Value = x.Value })
                    .ToArray();

                return Ok(new ClaimsWrapperDTO
                {
                    Claims = claims
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery(Name = "like")] string userName)
        {
            var users = await _userRepository.SearchByUserName(userName, HttpContext);

            return Ok(users);
        }

        [Authorize]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetUserById([FromRoute] Guid id)
        {
            var user = await _userRepository.GetById(id);

            return Ok(user);
        }

        [Authorize]
        [HttpGet("{userName}")]
        public async Task<IActionResult> GetUserByName([FromRoute] string userName)
        {
            var user = await _userRepository.GetByUserName(userName);

            return Ok(user);
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromForm] string? FirstName, [FromForm] string? Surname, [FromForm] string? UserName, [FromForm] IFormFile? ProfilePhoto)
        {
            try
            {
                var userUpdateDTO = new UserDTO
                {
                    FirstName = FirstName,
                    Surname = Surname,
                    UserName = UserName
                };

                if (ProfilePhoto != null && ProfilePhoto.Length > 0)
                {
                    using var memoryStream = new MemoryStream();
                    await ProfilePhoto.CopyToAsync(memoryStream);
                    userUpdateDTO.ProfilePhoto = memoryStream.ToArray();
                }

                await _userRepository.UpdateUserProfile(userUpdateDTO, HttpContext);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            var chatRooms = await _context.DirectChatRooms
                .Include(x => x.ChatMembers)
                    .ThenInclude(x => x.User)
                .Where(x => x.ChatMembers.Any(cm => cm.User.UserName == HttpContext.User.Identity!.Name))
                    .Select(x => x.Id).ToListAsync();

            foreach (var chatRoomId in chatRooms)
            {
                var chatRoomResult = await _chatRoomRepository.GetChatRoom(chatRoomId, HttpContext);

                if (!chatRoomResult.Success)
                {
                    continue;
                }

                var otherUser = chatRoomResult.ChatRoom.ChatMembers
                    .FirstOrDefault(x => x.User.UserName != HttpContext.User.Identity!.Name)?.User;

                if (otherUser?.UserName == null)
                {
                    continue;
                }

                try
                {
                    await _chatHubContext.Clients.User(otherUser.UserName).SendAsync("ChatChanged", chatRoomResult.ChatRoom);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send ChatChanged to {otherUser.UserName}: {ex.Message}");
                }
            }

            return Ok();
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            try
            {
                var userName = HttpContext.User.Identity!.Name!;
                var user = await _userRepository.GetByUserName(userName);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPost]
        [Route("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordDTO userChangePasswordDTO)
        {
            if (userChangePasswordDTO.NewPassword != userChangePasswordDTO.ConfirmNewPassword)
            {
                return BadRequest(new { message = "New password and confirmation do not match." });
            }

            try
            {
                await _authorizationService.ChangePassword(userChangePasswordDTO.CurrentPassword, userChangePasswordDTO.NewPassword);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
