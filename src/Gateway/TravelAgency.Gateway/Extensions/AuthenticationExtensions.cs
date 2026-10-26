using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Gateway.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        return services.AddJwtAuthentication(configuration);
    }

    public static IServiceCollection AddGatewayAuthorization(
        this IServiceCollection services)
    {
        services.AddAuthorizationBuilder().AddRoleHierarchyPolicies();

        return services;
    }
}
