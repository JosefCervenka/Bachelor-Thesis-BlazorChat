using BlazorChatApp.Application.DTOs.Users;
using Microsoft.AspNetCore.Components.Authorization;
using System.Net.Http.Json;
using System.Security.Claims;

namespace BlazorChatApp.Application.Services
{
    public class ClientAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;

        public ClientAuthenticationStateProvider(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            ClaimDTO[] claimDTOs = [];

            try
            {
                claimDTOs = (await _httpClient.GetFromJsonAsync<ClaimsWrapperDTO>("api/user/auth")).Claims;
            }
            catch (Exception e)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            if (claimDTOs is null or [])
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = claimDTOs.Select(c => new Claim(c.Type, c.Value));
            var identity = new ClaimsIdentity(claims, "auth");
            var principal = new ClaimsPrincipal(identity);

            return new AuthenticationState(principal);
        }
    }
}
