using TravelAgency.Booking.API.Settings;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Booking.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddBookingAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));
        return services.AddJwtAuthentication(configuration);
    }

    public static IServiceCollection AddBookingAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder().AddRoleHierarchyPolicies();

        return services;
    }
}
