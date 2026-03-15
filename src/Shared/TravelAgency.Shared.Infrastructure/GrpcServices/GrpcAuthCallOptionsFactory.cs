using Grpc.Core;
using Microsoft.Extensions.Configuration;

namespace TravelAgency.Shared.Infrastructure.GrpcServices;

/// <summary>
/// Factory that creates gRPC <see cref="CallOptions"/> with the internal service auth header.
/// Reads <c>GrpcSettings:InternalServiceToken</c> and throws if null/empty (fail-fast).
/// </summary>
public interface IGrpcAuthCallOptionsFactory
{
    /// <summary>
    /// Creates <see cref="CallOptions"/> with the x-internal-auth header for service-to-service calls.
    /// </summary>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>CallOptions with auth metadata.</returns>
    /// <exception cref="InvalidOperationException">Thrown when GrpcSettings:InternalServiceToken is not configured.</exception>
    CallOptions Create(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation that reads <c>GrpcSettings:InternalServiceToken</c> from configuration
/// and creates <see cref="CallOptions"/> with the auth header. Fails fast when token is missing.
/// </summary>
public sealed class GrpcAuthCallOptionsFactory(IConfiguration configuration) : IGrpcAuthCallOptionsFactory
{
    private const string AuthHeader = "x-internal-auth";
    private const string ConfigKey = "GrpcSettings:InternalServiceToken";

    /// <inheritdoc />
    public CallOptions Create(CancellationToken cancellationToken = default)
    {
        var token = configuration[ConfigKey];
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                "GrpcSettings:InternalServiceToken is required for gRPC service-to-service auth. " +
                "Set GrpcSettings__InternalServiceToken environment variable or add it to configuration.");

        var metadata = new Metadata { { AuthHeader, token } };
        return new CallOptions(metadata, deadline: null, cancellationToken);
    }
}
