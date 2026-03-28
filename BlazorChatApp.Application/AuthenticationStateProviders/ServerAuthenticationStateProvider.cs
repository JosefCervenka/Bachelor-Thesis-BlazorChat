using BlazorChatApp.Application.Services.SecurityServices;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace BlazorChatApp.Application.AuthenticationStateProviders
{
    public class ServerAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly AuthorizationService _authorizationService;

        public ServerAuthenticationStateProvider(AuthorizationService authorizationService)
        {
            _authorizationService = authorizationService;
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var principal = await _authorizationService.GetCredentials();

                if (principal is null || principal.Claims.ToArray() is [])
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                return await Task.FromResult(new AuthenticationState(principal));
            }
            catch (Exception e)
            {
                return await Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
            }
        }
    }
}
