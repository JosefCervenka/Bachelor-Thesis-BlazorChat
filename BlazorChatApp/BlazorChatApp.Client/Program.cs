using BlazorChatApp.Application.Services;
using BlazorChatApp.Application.Services.ChatServices;
using BlazorChatApp.Application.Services.InvitationServices;
using BlazorChatApp.Application.Services.RenderModes;
using BlazorChatApp.Application.Services.SignalRServices;
using BlazorChatApp.Application.Services.UserServices;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddMudServices();

builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ClientAuthenticationStateProvider>();
builder.Services.AddScoped<ISignalRService, ClientSignalRService>();
builder.Services.AddSingleton<IRenderModel, ClientRenderModel>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<ChatRoomService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

var host = builder.Build();

var invitationService = host.Services.GetRequiredService<InvitationService>();

await host.RunAsync();