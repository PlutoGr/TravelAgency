using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Gateway.Extensions;

public static class CorsExtensions
{
    public const string GatewayCorsPolicyName = TravelAgency.Shared.Infrastructure.Extensions.CorsExtensions.GatewayCorsPolicyName;

    public static IServiceCollection AddGatewayCors(this IServiceCollection services, IConfiguration configuration)
        => services.AddSharedCorsForGateway(configuration);
}
