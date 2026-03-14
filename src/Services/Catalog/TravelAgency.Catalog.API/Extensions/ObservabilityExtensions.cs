using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace TravelAgency.Catalog.API.Extensions;

public static class ObservabilityExtensions
{
    public static void AddCatalogSerilog(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));
    }

    public static IServiceCollection AddCatalogTracing(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TravelAgency.Catalog")));
        return services;
    }
}
