using TravelAgency.Identity.Application.Settings;
using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Identity.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddIdentityAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        return services.AddJwtAuthentication(configuration);
    }

    public static IServiceCollection AddIdentityAuthorization(this IServiceCollection services)
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
