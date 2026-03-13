using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace TravelAgency.Gateway.Extensions;

public static class ObservabilityExtensions
{
    public static IHostBuilder AddGatewaySerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog((context, configuration) =>
        {
            configuration.ReadFrom.Configuration(context.Configuration);
        });

        return hostBuilder;
    }

    public static IServiceCollection AddGatewayTracing(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource =>
                resource.AddService("TravelAgency.Gateway"))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation();
            });

        return services;
    }
}
