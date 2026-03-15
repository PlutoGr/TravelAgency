using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.Transforms;

namespace TravelAgency.Gateway.Extensions;

public static class YarpExtensions
{
    public static IServiceCollection AddGatewayYarp(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CookieSettings>(configuration.GetSection(CookieSettings.SectionName));
        services.Configure<AuthRouteSettings>(configuration.GetSection(AuthRouteSettings.SectionName));
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"))
            .AddTransforms<AuthResponseTransformProvider>()
            .AddTransforms<AuthRequestTransformProvider>();

        return services;
    }

    public static WebApplication MapGatewayYarp(this WebApplication app)
    {
        app.MapReverseProxy();
        return app;
    }
}
