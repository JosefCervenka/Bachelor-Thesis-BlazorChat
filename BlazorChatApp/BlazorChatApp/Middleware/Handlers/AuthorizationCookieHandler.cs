using BlazorChatApp.Application.Services.SecurityServices;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace BlazorChatApp.Middleware.Handlers
{
    public class AuthorizationCookieHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly AuthorizationService _authorizationService;

        public AuthorizationCookieHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, AuthorizationService authorizationService)
            : base(options, logger, encoder)
        {
            _authorizationService = authorizationService;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var authState = await _authorizationService.GetCredentials();

            context.User = authState;

            await next.Invoke(context);
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var user = await _authorizationService.GetCredentials();

            if (user?.Identity?.IsAuthenticated == true)
            {
                var ticket = new AuthenticationTicket(user, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.NoResult();
        }
    }
}
