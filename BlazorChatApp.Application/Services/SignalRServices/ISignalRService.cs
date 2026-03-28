using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorChatApp.Application.Services.SignalRServices
{
    public interface ISignalRService
    {
        public Task<HubConnection> CreateConnection(string path);
    }
}
