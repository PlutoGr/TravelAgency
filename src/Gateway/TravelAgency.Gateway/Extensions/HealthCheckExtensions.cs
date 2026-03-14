using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TravelAgency.Gateway.HealthChecks;

namespace TravelAgency.Gateway.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("HealthChecks", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        var healthCheckBuilder = services.AddHealthChecks();

        var endpoints = configuration.GetSection("HealthCheckEndpoints");
        foreach (var endpoint in endpoints.GetChildren())
        {
            var url = endpoint.Value;
            if (string.IsNullOrEmpty(url))
                continue;

            var serviceName = endpoint.Key;
            var serviceUrl = url;

            healthCheckBuilder.Add(new HealthCheckRegistration(
                serviceName,
                sp =>
                {
                    var factory = sp.GetRequiredService<IHttpClientFactory>();
                    var logger = sp.GetRequiredService<ILogger<DownstreamServiceHealthCheck>>();
                    return new DownstreamServiceHealthCheck(factory, serviceUrl, logger);
                },
                failureStatus: HealthStatus.Degraded,
                tags: ["ready"]));
        }

        return services;
    }

    public static WebApplication MapGatewayHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }
}
