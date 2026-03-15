using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Media.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddMediaAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddJwtAuthentication(configuration);
    }

    public static IServiceCollection AddMediaAuthorization(this IServiceCollection services)
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
