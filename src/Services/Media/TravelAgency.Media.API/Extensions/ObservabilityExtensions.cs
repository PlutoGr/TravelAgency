using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace TravelAgency.Media.API.Extensions;

public static class ObservabilityExtensions
{
    public static IHostBuilder AddMediaSerilog(this IHostBuilder builder)
    {
        // Sinks come only from configuration (Serilog:WriteTo in appsettings.json).
        // A second WriteTo.Console() here printed every log line twice (issue #57).
        builder.UseSerilog((context, config) =>
        {
            config.ReadFrom.Configuration(context.Configuration)
                  .Enrich.FromLogContext();
        });
        return builder;
    }

    public static IServiceCollection AddMediaTracing(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("TravelAgency.Media")));
        return services;
    }
}
