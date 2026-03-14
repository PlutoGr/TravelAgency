using System.Security.Cryptography;
using System.Text;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Configuration;

namespace TravelAgency.Identity.Infrastructure.GrpcServices;

/// <summary>
/// Server-side interceptor that enforces service-to-service authentication on all gRPC calls.
/// Callers must supply an <c>x-internal-auth</c> metadata header whose value matches
/// <c>GrpcSettings:InternalServiceToken</c> (configure via environment variable
/// <c>GrpcSettings__InternalServiceToken</c>).
/// </summary>
public sealed class GrpcAuthInterceptor(IConfiguration configuration) : Interceptor
{
    private const string AuthHeader = "x-internal-auth";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ValidateInternalToken(context);
        return await continuation(request, context);
    }

    public override async Task ServerStreamingServerHandler<TRequest, TResponse>(
        TRequest request,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        ServerStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ValidateInternalToken(context);
        await continuation(request, responseStream, context);
    }

    public override async Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        ServerCallContext context,
        ClientStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ValidateInternalToken(context);
        return await continuation(requestStream, context);
    }

    public override async Task DuplexStreamingServerHandler<TRequest, TResponse>(
        IAsyncStreamReader<TRequest> requestStream,
        IServerStreamWriter<TResponse> responseStream,
        ServerCallContext context,
        DuplexStreamingServerMethod<TRequest, TResponse> continuation)
    {
        ValidateInternalToken(context);
        await continuation(requestStream, responseStream, context);
    }

    private void ValidateInternalToken(ServerCallContext context)
    {
        var expectedToken = configuration["GrpcSettings:InternalServiceToken"];
        if (string.IsNullOrWhiteSpace(expectedToken))
            throw new RpcException(new Status(
                StatusCode.Internal,
                "gRPC internal service token is not configured. Set GrpcSettings__InternalServiceToken."));

        var entry = context.RequestHeaders.FirstOrDefault(
            h => string.Equals(h.Key, AuthHeader, StringComparison.OrdinalIgnoreCase));

        if (entry is null)
            throw new RpcException(new Status(
                StatusCode.Unauthenticated,
                $"Missing required '{AuthHeader}' header."));

        var token = entry.Value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? entry.Value["Bearer ".Length..]
            : entry.Value;

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(token),
                Encoding.UTF8.GetBytes(expectedToken)))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Invalid internal service token."));
    }
}
