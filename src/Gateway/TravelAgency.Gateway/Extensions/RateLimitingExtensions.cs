using AspNetCoreRateLimit;

namespace TravelAgency.Gateway.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.Configure<IpRateLimitPolicies>(configuration.GetSection("IpRateLimitPolicies"));
        services.AddInMemoryRateLimiting();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

        return services;
    }
}
