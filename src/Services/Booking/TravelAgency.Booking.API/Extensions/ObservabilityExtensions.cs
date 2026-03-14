using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace TravelAgency.Booking.API.Extensions;

public static class ObservabilityExtensions
{
    public static void AddBookingSerilog(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.WithMachineName()
                .Enrich.FromLogContext();
        });
    }

    public static IServiceCollection AddBookingTracing(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("booking-service")));

        return services;
    }
}
