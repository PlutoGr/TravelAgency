using TravelAgency.Identity.Application.Settings;
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
        services.AddAuthorizationBuilder().AddRoleHierarchyPolicies();

        return services;
    }
}
