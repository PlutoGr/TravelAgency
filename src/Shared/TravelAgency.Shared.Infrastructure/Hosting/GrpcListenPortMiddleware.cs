using Microsoft.Extensions.Options;

namespace TravelAgency.Shared.Infrastructure.Hosting;

/// <summary>
/// Keeps the two listeners from serving each other's traffic.
/// The gRPC port accepts only gRPC (content type application/grpc).
/// The public HTTP port accepts only ordinary HTTP. The local port is taken from the
/// socket, so a forged Host header cannot move a call onto the other listener.
/// An in-memory server (no socket, local port 0) is left alone so TestServer tests keep working.
/// </summary>
public sealed class GrpcListenPortMiddleware(RequestDelegate next, IOptions<ServiceListenSettings> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var localPort = context.Connection.LocalPort;
        if (localPort == 0)
        {
            await next(context);
            return;
        }

        var onGrpcPort = localPort == options.Value.GrpcPort;
        var isGrpc = context.Request.ContentType?.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase) == true;
        if (onGrpcPort != isGrpc)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context);
    }
}
