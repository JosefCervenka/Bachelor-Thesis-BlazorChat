using BlazorChatApp.Application.DTOs.ChatRequests;
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
    public class InvitationController : ControllerBase
    {
        private readonly InvitationRepository _invitationRepository;
        private readonly IHubContext<InvitationHub> _invitationHubContext;
        private readonly IHubContext<ChatRoomHub> _chatHubContext;

        public InvitationController(IHubContext<InvitationHub> invitationHubContext, IHubContext<ChatRoomHub> chatHubContext, AppDbContext context)
        {
            _invitationHubContext = invitationHubContext;
            _chatHubContext = chatHubContext;
            _invitationRepository = new InvitationRepository(context);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            var requestDTO = await _invitationRepository.Delete(id, HttpContext);

            if (requestDTO is null)
                return BadRequest();

            _invitationHubContext?.Clients.User(requestDTO.Invited.UserName).SendAsync("GroupChatRequestRemoved", requestDTO);
            _invitationHubContext?.Clients.User(requestDTO.Initializer.UserName).SendAsync("GroupChatRequestRemoved", requestDTO);
            return Ok();
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateChatRequest([FromBody] ChatRequestDTO chatRequestDTO)
        {
            var (result, errorMessage) = await _invitationRepository.CreateChatRequest(chatRequestDTO, HttpContext);

            if (result is null)
                return BadRequest(new { message = errorMessage });

            if (result is FriendRequestDTO friendRequestDTO)
            {
                _invitationHubContext?.Clients.User(result.Invited.UserName).SendAsync("ChatRequestInvited", result);
                _invitationHubContext?.Clients.User(result.Initializer.UserName).SendAsync("ChatRequestInitialized", result);
            }
            else if (result is GroupChatRequestDTO groupRequestDTO)
            {
                _invitationHubContext?.Clients.User(result.Invited.UserName).SendAsync("ChatRequestInvited", result);
            }

            return Ok(result);
        }

        [HttpGet("initialized")]
        public async Task<IActionResult> GetInitializedChatRequests()
        {
            var allRequests = await _invitationRepository.GetInitializedChatRequests(HttpContext);

            return Ok(new ChatRequestWrapperDTO
            {
                ChatRequests = allRequests
            });
        }

        [HttpGet("invited")]
        public async Task<IActionResult> GetInvitedChatRequests()
        {
            var allRequests = await _invitationRepository.GetInvitedChatRequests(HttpContext);

            return Ok(new ChatRequestWrapperDTO
            {
                ChatRequests = allRequests
            });
        }

        [HttpPost]
        [Route("response")]
        public async Task<IActionResult> ResponseInvitation([FromBody] ChatRequestDTO chatRequestDTO)
        {
            var (request, directChatDTO, groupChatDTO) = await _invitationRepository.ResponseInvitation(chatRequestDTO, HttpContext);

            if (request is null)
                return BadRequest();

            _invitationHubContext?.Clients.User(request.Invited.UserName).SendAsync("ChatRequestResponseInvited", request);

            if (groupChatDTO is not null)
            {
                _chatHubContext?.Clients.User(request.Invited.UserName).SendAsync("ChatAccepted", groupChatDTO);
            }

            if (chatRequestDTO is FriendRequestDTO friendRequestDTO)
            {
                _invitationHubContext?.Clients.User(request.Initializer.UserName).SendAsync("ChatRequestResponseInitialized", friendRequestDTO);

                if (directChatDTO is not null)
                {
                    _chatHubContext?.Clients.User(request.Invited.UserName).SendAsync("ChatAccepted", directChatDTO);
                    _chatHubContext?.Clients.User(request.Initializer.UserName).SendAsync("ChatAccepted", directChatDTO);
                }
            }

            return Ok();
        }
    }
}
