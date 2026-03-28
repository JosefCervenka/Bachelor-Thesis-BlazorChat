using BlazorChatApp.Application.AuthenticationStateProviders;
using BlazorChatApp.Application.Services.ChatServices;
using BlazorChatApp.Application.Services.HttpClientFactory;
using BlazorChatApp.Application.Services.InvitationServices;
using BlazorChatApp.Application.Services.RenderModes;
using BlazorChatApp.Application.Services.SecurityServices;
using BlazorChatApp.Application.Services.SignalRServices;
using BlazorChatApp.Application.Services.UserServices;
using BlazorChatApp.Hubs;
using BlazorChatApp.Infrastructure;
using BlazorChatApp.Middleware.Handlers;
using BlazorChatApp.Presentation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using System.Text.Json.Serialization;


var builder = WebApplication.CreateBuilder(args);

//service
builder.Services.AddSingleton<PasswordService>();
builder.Services.AddSingleton<JwtService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<ISignalRService, ServerSignalRService>();
builder.Services.AddSingleton<IRenderModel, ServerRenderModel>();
builder.Services.AddScoped<InvitationService>();
builder.Services.AddScoped<ChatRoomService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<IUserIdProvider, UserIdProvider>();

//context
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration["dbConnectionString"]);

    var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.None));
    options.UseLoggerFactory(loggerFactory);
});

//controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
        options.JsonSerializerOptions.PropertyNamingPolicy = null; // Keep PascalCase
    });

//Cookie auth handlerer
builder.Services.AddAuthentication("CustomCookie")
    .AddScheme<AuthenticationSchemeOptions, AuthorizationCookieHandler>("CustomCookie", options => { });

//Auth
builder.Services.AddAuthorization();

//state provider
builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();

builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10MB
});

builder.Services.AddMudServices();
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        ["application/octet-stream"]);
});

builder.Services.AddCors(options =>
{
    var corsOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>();

    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//HttpClient configuration with jwt token added to cookies
builder.Services.AddScoped<ServerHttpClientFactory>();
builder.Services.AddScoped<HttpClient>(sp =>
{
    var factory = sp.GetRequiredService<ServerHttpClientFactory>();
    return factory.CreateClient();
});


// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

var app = builder.Build();

app.Use((context, next) =>
{
    context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    return next();
});

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

if (!app.Environment.IsDevelopment())
{
    app.UseStatusCodePagesWithReExecute("/not-found");
}

app.UseHttpsRedirection();
app.MapControllers();

app.UseResponseCompression();


//Hubs mapping
app.MapHub<ChatRoomHub>("/chatroom");
app.MapHub<InvitationHub>("/notification");
app.MapHub<ChatHub>("/chat");


app.MapStaticAssets();

app.UseStaticFiles(new StaticFileOptions()
{
    ServeUnknownFileTypes = true,
    DefaultContentType = "application/octet-stream",
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers.Append("Cross-Origin-Embedder-Policy", "require-corp");
        ctx.Context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    }
});

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorChatApp.Client._Imports).Assembly);

app.Run();