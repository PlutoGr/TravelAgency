using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TravelAgency.Chat.API.Extensions;

public static class ObservabilityExtensions
{
    public static void AddChatSerilog(this ConfigureHostBuilder host)
    {
        host.UseSerilog((context, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(context.Configuration)
                .Enrich.WithMachineName()
                .Enrich.FromLogContext();
        });
    }

    public static IServiceCollection AddChatTracing(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("chat-service")));

        return services;
    }
}
