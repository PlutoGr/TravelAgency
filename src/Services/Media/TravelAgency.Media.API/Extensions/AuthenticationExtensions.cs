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
        services.AddAuthorizationBuilder().AddRoleHierarchyPolicies();

        return services;
    }
}
