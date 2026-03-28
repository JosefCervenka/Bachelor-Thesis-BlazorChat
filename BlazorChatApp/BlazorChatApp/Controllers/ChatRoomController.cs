using BlazorChatApp.Application.DTOs.ChatMembers;
using BlazorChatApp.Application.DTOs.ChatRooms;
using BlazorChatApp.Application.Services.InvitationServices;
using BlazorChatApp.Hubs;
using BlazorChatApp.Infrastructure;
using BlazorChatApp.Application.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BlazorChatApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class ChatRoomController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly InvitationService _invitationService;
        private readonly IHubContext<ChatRoomHub> _chatHubContext;
        private readonly ChatRoomRepository _chatRoomRepository;
        private readonly IHubContext<InvitationHub> _invitationHubContext;

        public ChatRoomController(AppDbContext context, InvitationService invitationService, IHubContext<ChatRoomHub> chatHubContext, IHubContext<InvitationHub> invitationHubContext)
        {
            _context = context;
            _invitationService = invitationService;
            _chatHubContext = chatHubContext;
            _invitationHubContext = invitationHubContext;
            _chatRoomRepository = new ChatRoomRepository(_context);
        }

        [HttpDelete("{chatId}/remove-member/{chatMemberId}")]

        public async Task<IActionResult> RemoveUser([FromRoute] Guid chatId, [FromRoute] Guid chatMemberId)
        {
            var result = await _chatRoomRepository.RemoveUserFromChat(chatId, chatMemberId, HttpContext);

            if (!result.Success)
            {
                if (result.ErrorMessage.Contains("Forbidden"))
                    return Forbid();
                if (result.ErrorMessage.Contains("not found"))
                    return NotFound();
                return BadRequest(result.ErrorMessage);
            }

            _chatHubContext?.Clients.User(result.RemovedMember.User.UserName).SendAsync("ChatUserRemove", chatId);

            return Ok();
        }

        [HttpPost("{chatId}/change-member-role/{chatMemberId}")]
        public async Task<IActionResult> ChangeUserRole([FromRoute] Guid chatId, [FromRoute] Guid chatMemberId, [FromBody] ChatMemberRoleDTO chatMemberRoleDTO)
        {
            var result = await _chatRoomRepository.ChangeUserRole(chatId, chatMemberId, chatMemberRoleDTO.Id, HttpContext);

            if (!result.Success)
            {
                if (result.ErrorMessage.Contains("Forbidden"))
                    return Forbid();
                if (result.ErrorMessage.Contains("not found"))
                    return NotFound();
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.UpdatedMember);
        }

        [HttpDelete("{chatId}")]
        public async Task<IActionResult> Delete([FromRoute] Guid chatId)
        {
            var result = await _chatRoomRepository.DeleteChatRoom(chatId, HttpContext);

            if (!result.Success)
            {
                if (result.ErrorMessage.Contains("Forbidden"))
                    return Forbid();
                if (result.ErrorMessage.Contains("not found"))
                    return NotFound();
                return BadRequest(result.ErrorMessage);
            }

            foreach (var chatMember in result.ChatRoom.ChatMembers)
            {
                _chatHubContext?.Clients.User(chatMember!.User.UserName).SendAsync("ChatUserRemove", chatId);
            }

            foreach (var request in result.GroupChatRequests)
            {
                _invitationHubContext?.Clients.User(request.Invited.UserName).SendAsync("GroupChatRequestRemoved", request);
            }

            return Ok();
        }

        [HttpPost("chat")]
        public async Task<IActionResult> Create([FromBody] GroupChatRoomDTO groupChatRoomDTO)
        {
            var currentUser = _context.Users.FirstOrDefault(x => x.UserName == HttpContext!.User!.Identity!.Name);

            var userIds = groupChatRoomDTO.ChatMembers
                .Select(x => x.User.Id)
                .ToList();

            groupChatRoomDTO = await _chatRoomRepository.Save(groupChatRoomDTO, HttpContext);

            _chatHubContext?.Clients.User(currentUser!.UserName).SendAsync("ChatAccepted", groupChatRoomDTO);

            return Ok(groupChatRoomDTO);
        }


        [HttpGet("chats")]
        public async Task<IActionResult> Get()
        {
            var chats = await _chatRoomRepository.GetWhereMember(HttpContext);

            return Ok(new ChatRoomWrapperDTO
            {
                ChatRooms = chats
            });
        }

        [HttpGet("{chatId}")]
        public async Task<IActionResult> GetChatRoom([FromRoute] Guid chatId)
        {
            var result = await _chatRoomRepository.GetChatRoom(chatId, HttpContext);
            if (!result.Success)
            {
                if (result.ErrorMessage.Contains("Forbidden"))
                    return Forbid();
                if (result.ErrorMessage.Contains("not found"))
                    return NotFound();
                return BadRequest(result.ErrorMessage);
            }
            return Ok(result.ChatRoom);
        }

        [HttpGet("members/{chatRoomId}")]
        public async Task<IActionResult> GetChatMembers([FromRoute] Guid chatRoomId)
        {
            var result = await _chatRoomRepository.GetChatMembers(chatRoomId, HttpContext);

            if (!result.Success)
            {
                if (result.ErrorMessage.Contains("Forbidden"))
                    return Forbid();
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.ChatMembers);
        }

        [HttpGet("membership/{chatRoomId}")]
        public async Task<IActionResult> MemberOfChatRoom([FromRoute] Guid chatRoomId)
        {
            var result = await _chatRoomRepository.GetUserMemberShip(chatRoomId, HttpContext);

            if (!result.Success)
            {
                if (result.ErrorMessage.Contains("not a member"))
                    return NotFound(result.ErrorMessage);
                if (result.ErrorMessage.Contains("User not found"))
                    return Unauthorized();
                return BadRequest(result.ErrorMessage);
            }

            return Ok(result.MemberShip);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateChatRoom([FromBody] GroupChatRoomDTO groupChatRoomDTO)
        {
            var result = await _chatRoomRepository.Update(groupChatRoomDTO, HttpContext);
            if (!result.Success)
            {
                if (result.ErrorMessage.Contains("Forbidden"))
                    return Forbid();
                if (result.ErrorMessage.Contains("not found"))
                    return NotFound();
                return BadRequest(result.ErrorMessage);
            }
            var updatedChat = result.groupChatRoomDTO;

            foreach (var chatMember in updatedChat.ChatMembers)
            {
                await _chatHubContext.Clients.User(chatMember.User.UserName).SendAsync("ChatChanged", updatedChat);
            }

            return Ok(updatedChat);
        }
    }
}
