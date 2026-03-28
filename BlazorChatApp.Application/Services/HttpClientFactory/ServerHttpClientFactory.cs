using BlazorChatApp.Application.Services.SecurityServices;
using BlazorChatApp.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net;

namespace BlazorChatApp.Application.Services.HttpClientFactory
{
    public class ServerHttpClientFactory
    {
        private readonly JwtService _jwtService;
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public ServerHttpClientFactory(
            JwtService jwtService,
            AppDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _jwtService = jwtService;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        public HttpClient CreateClient()
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext?.User?.Identity?.Name is null)
            {
                return new HttpClient
                {
                    BaseAddress = new Uri(_configuration["Server:BaseUrl"]!)
                };
            }

            var user = _context.Users.FirstOrDefault(x => x.UserName == httpContext.User.Identity.Name);

            if (user == null)
            {
                return new HttpClient
                {
                    BaseAddress = new Uri(_configuration["Server:BaseUrl"]!)
                };
            }

            var token = _jwtService.GenerateJwtToken(
                user,
                _configuration["jwt:secret"]!,
                _configuration["jwt:issuer"]!,
                _configuration["jwt:audience"]!
            );

            var cookieContainer = new CookieContainer();
            var cookie = new Cookie("jwt", token, "/", _configuration["Server:Domain"]!);
            cookieContainer.Add(cookie);

            var handler = new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer
            };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(_configuration["Server:BaseUrl"]!)
            };
        }
    }
}
