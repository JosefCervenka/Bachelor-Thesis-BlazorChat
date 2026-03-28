using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BlazorChatApp.Hubs
{
    [Authorize]
    public class InvitationHub : Hub
    {

    }
}
