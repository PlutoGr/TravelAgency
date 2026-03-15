using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TravelAgency.Identity.API.Logging;

namespace TravelAgency.Identity.API.Extensions;

public static class ObservabilityExtensions
{
    public static void AddIdentitySerilog(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, configuration) =>
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Destructure.With<PiiMaskingDestructuringPolicy>());
    }

    public static IServiceCollection AddIdentityTracing(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TravelAgency.Identity")));

        return services;
    }
}
