using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorChatApp.Application.Services.SignalRServices
{
    public class ClientSignalRService : ISignalRService, IAsyncDisposable
    {
        private readonly NavigationManager _navigationManager;

        private HubConnection? hubConnection;

        private string _path = string.Empty;
        public ClientSignalRService(NavigationManager navigationManager)
        {
            _navigationManager = navigationManager;
        }

        public async Task<HubConnection> CreateConnection(string path)
        {
            _path = path;

            hubConnection = new HubConnectionBuilder()
               .WithUrl($"{_navigationManager.BaseUri}{path}")
               .WithAutomaticReconnect()
               .Build();

            return hubConnection;
        }

        public async ValueTask DisposeAsync()
        {
            hubConnection?.DisposeAsync();
        }
    }
}
