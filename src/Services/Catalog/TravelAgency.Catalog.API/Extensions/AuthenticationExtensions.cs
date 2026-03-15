using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Catalog.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddCatalogAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddJwtAuthentication(configuration);
        services.AddAuthorizationBuilder()
            .AddPolicy("ManagerOrAdmin", policy =>
                policy.RequireRole(AppRoles.Manager, AppRoles.Admin));
        return services;
    }
}
