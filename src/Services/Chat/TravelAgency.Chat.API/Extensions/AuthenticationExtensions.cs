using Microsoft.AspNetCore.Authentication.JwtBearer;
using TravelAgency.Chat.API.Settings;
using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Chat.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddChatAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        return services.AddJwtAuthentication(configuration, options =>
        {
            // SignalR: accept token from query string for WebSocket connections
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken))
                        context.Token = accessToken;
                    return Task.CompletedTask;
                }
            };
        });
    }

    public static IServiceCollection AddChatAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(AuthPolicies.RequireAuthenticated, policy =>
                policy.RequireRole(AppRoles.Client, AppRoles.Manager, AppRoles.Admin))
            .AddPolicy(AuthPolicies.RequireManager, policy =>
                policy.RequireRole(AppRoles.Manager, AppRoles.Admin))
            .AddPolicy(AuthPolicies.RequireAdmin, policy =>
                policy.RequireRole(AppRoles.Admin))
            .AddPolicy(AuthPolicies.RequireManagerOrAdmin, policy =>
                policy.RequireRole(AppRoles.Manager, AppRoles.Admin));

        return services;
    }
}
