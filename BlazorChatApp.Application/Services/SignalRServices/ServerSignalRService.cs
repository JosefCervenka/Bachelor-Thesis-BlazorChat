using BlazorChatApp.Application.Services.SecurityServices;
using BlazorChatApp.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace BlazorChatApp.Application.Services.SignalRServices
{
    public class ServerSignalRService : ISignalRService, IAsyncDisposable
    {
        private readonly JwtService _jwtService;

        private readonly AppDbContext _context;

        private readonly AuthorizationService _authorizationService;

        private readonly IConfiguration _configuration;

        private HubConnection? hubConnection;

        private string _path = string.Empty;

        public ServerSignalRService(IConfiguration configuration, JwtService jwtService, AppDbContext context, AuthorizationService authorizationService)
        {
            _jwtService = jwtService;
            _context = context;
            _authorizationService = authorizationService;
            _configuration = configuration;
        }

        public async Task<HubConnection> CreateConnection(string path)
        {
            _path = path;
            var secret = _configuration["jwt:secret"];
            var issuer = _configuration["jwt:issuer"];
            var audience = _configuration["jwt:audience"];
            var baseUrl = _configuration["Server:BaseUrl"];
            var domain = _configuration["Server:Domain"];

            var credentials = await _authorizationService.GetCredentials();
            var userName = credentials?.Identity?.Name;

            Cookie? cookie = null;

            if (userName is not null)
            {
                var user = _context.Users.FirstOrDefault(x => x.UserName == userName);
                var token = _jwtService.GenerateJwtToken(user, secret, issuer, audience);

                cookie = new Cookie()
                {
                    Name = "jwt",
                    Value = token,
                    Domain = domain,
                    Path = "/",
                };
            }

            hubConnection = new HubConnectionBuilder()
                .WithUrl($"{baseUrl}/{path}", option =>
                {
                    if (cookie is not null)
                    {
                        option.Cookies.Add(cookie);
                    }
                })
                .WithAutomaticReconnect()
                .Build();

            return hubConnection;
        }

        public async ValueTask DisposeAsync()
        {
            Console.WriteLine($"Dispose SignalR {_path}");
            hubConnection?.DisposeAsync();
        }
    }
}
