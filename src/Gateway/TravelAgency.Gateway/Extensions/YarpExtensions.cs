namespace TravelAgency.Gateway.Extensions;

public static class YarpExtensions
{
    public static IServiceCollection AddGatewayYarp(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));

        return services;
    }

    public static WebApplication MapGatewayYarp(this WebApplication app)
    {
        app.MapReverseProxy();
        return app;
    }
}
