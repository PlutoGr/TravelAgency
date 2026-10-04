namespace TravelAgency.Media.API.Hosting;

/// <summary>
/// Keeps the two listeners from serving each other's traffic.
/// Port <see cref="MediaPorts.Grpc"/> accepts only gRPC (content type application/grpc).
/// The public HTTP port accepts only ordinary HTTP. The local port is taken from the
/// socket, so a forged Host header cannot move a call onto the other listener.
/// </summary>
internal sealed class GrpcListenPortMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var onGrpcPort = context.Connection.LocalPort == MediaPorts.Grpc;
        var isGrpc = context.Request.ContentType?.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase) == true;
        if (onGrpcPort != isGrpc)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }

        await next(context);
    }
}
