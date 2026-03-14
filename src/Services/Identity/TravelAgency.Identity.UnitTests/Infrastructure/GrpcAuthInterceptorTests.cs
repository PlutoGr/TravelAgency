using Grpc.Core;
using Microsoft.Extensions.Configuration;
using TravelAgency.Identity.Infrastructure.GrpcServices;

namespace TravelAgency.Identity.UnitTests.Infrastructure;

/// <summary>
/// Covers FIX-003: GrpcAuthInterceptor validates the x-internal-auth header against
/// GrpcSettings:InternalServiceToken on every inbound gRPC call.
/// </summary>
public class GrpcAuthInterceptorTests
{
    private static GrpcAuthInterceptor CreateInterceptor(string? configuredToken) =>
        new(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["GrpcSettings:InternalServiceToken"] = configuredToken
            })
            .Build());

    private static ServerCallContext CreateContext(string? headerValue)
    {
        var headers = new Metadata();
        if (headerValue is not null)
            headers.Add("x-internal-auth", headerValue);
        return new TestServerCallContext(headers);
    }

    [Fact]
    public async Task UnaryServerHandler_WhenTokenNotConfigured_ThrowsRpcExceptionInternal()
    {
        var interceptor = CreateInterceptor(configuredToken: "");
        var context = CreateContext("any-token");
        UnaryServerMethod<object, object> continuation = (_, _) => Task.FromResult(new object());

        var act = () => interceptor.UnaryServerHandler(new object(), context, continuation);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Internal);
    }

    [Fact]
    public async Task UnaryServerHandler_WhenAuthHeaderMissing_ThrowsRpcExceptionUnauthenticated()
    {
        var interceptor = CreateInterceptor("secret-token");
        var context = CreateContext(headerValue: null);
        UnaryServerMethod<object, object> continuation = (_, _) => Task.FromResult(new object());

        var act = () => interceptor.UnaryServerHandler(new object(), context, continuation);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unauthenticated);
    }

    [Fact]
    public async Task UnaryServerHandler_WhenHeaderValueDoesNotMatchToken_ThrowsRpcExceptionUnauthenticated()
    {
        var interceptor = CreateInterceptor("secret-token");
        var context = CreateContext("wrong-token");
        UnaryServerMethod<object, object> continuation = (_, _) => Task.FromResult(new object());

        var act = () => interceptor.UnaryServerHandler(new object(), context, continuation);

        await act.Should().ThrowAsync<RpcException>()
            .Where(e => e.StatusCode == StatusCode.Unauthenticated);
    }

    [Theory]
    [InlineData("secret-token")]
    [InlineData("Bearer secret-token")]
    public async Task UnaryServerHandler_WhenHeaderValueMatchesToken_CallsContinuation(string headerValue)
    {
        var interceptor = CreateInterceptor("secret-token");
        var context = CreateContext(headerValue);

        var continuationCalled = false;
        UnaryServerMethod<object, object> continuation = (_, _) =>
        {
            continuationCalled = true;
            return Task.FromResult(new object());
        };

        await interceptor.UnaryServerHandler(new object(), context, continuation);

        continuationCalled.Should().BeTrue();
    }

    /// <summary>
    /// Concrete stub of the abstract <see cref="ServerCallContext"/> that supplies
    /// caller-controlled request headers while providing no-op stubs for all other
    /// abstract members. Moq cannot set up the non-virtual <c>RequestHeaders</c>
    /// property because it delegates internally to the protected abstract
    /// <c>RequestHeadersCore</c>; a hand-written subclass sidesteps the issue entirely.
    /// </summary>
    private sealed class TestServerCallContext(Metadata requestHeaders) : ServerCallContext
    {
        protected override string MethodCore => string.Empty;
        protected override string HostCore => string.Empty;
        protected override string PeerCore => string.Empty;
        protected override DateTime DeadlineCore => DateTime.MaxValue;
        protected override Metadata RequestHeadersCore => requestHeaders;
        protected override CancellationToken CancellationTokenCore => CancellationToken.None;
        protected override Metadata ResponseTrailersCore => [];
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore =>
            new(null, new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(
            ContextPropagationOptions? options) => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) =>
            Task.CompletedTask;
    }
}
