using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TravelAgency.Shared.Infrastructure.Hosting;

public static class GrpcEndpointMappingExtensions
{
    /// <summary>
    /// Maps a gRPC service and restricts it to the gRPC listen port (<c>*:8081</c> by default).
    /// The Host header must name that port. Combined with <see cref="GrpcListenPortMiddleware"/>,
    /// which checks the socket, a call cannot switch listeners by forging Host.
    /// TestServer has no socket and no Host port, so the host restriction is skipped there.
    /// </summary>
    public static GrpcServiceEndpointConventionBuilder MapGrpcServiceOnGrpcPort<TService>(
        this IEndpointRouteBuilder endpoints)
        where TService : class
    {
        var settings = endpoints.ServiceProvider.GetRequiredService<IOptions<ServiceListenSettings>>().Value;
        var port = settings.GrpcPort > 0 ? settings.GrpcPort : ServiceListenSettings.DefaultGrpcPort;
        var mapped = endpoints.MapGrpcService<TService>();
        if (!IsTestServer(endpoints.ServiceProvider))
            mapped.RequireHost($"*:{port}");
        return mapped;
    }

    private static bool IsTestServer(IServiceProvider services)
    {
        var server = services.GetService<IServer>();
        var name = server?.GetType().FullName;
        return name is not null && name.Contains("Microsoft.AspNetCore.TestHost", StringComparison.Ordinal);
    }
}
