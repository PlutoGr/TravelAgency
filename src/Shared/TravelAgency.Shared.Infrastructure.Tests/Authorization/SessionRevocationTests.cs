using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.Authorization;

namespace TravelAgency.Shared.Infrastructure.Tests.Authorization;

public class SessionRevocationTests
{
    [Theory]
    [InlineData("3|1", 3, true)]
    [InlineData("0|0", 0, false)]
    public void TryParse_ReadsVersionAndFlag(string value, long version, bool blocked)
    {
        SessionRevocationNames.TryParse(value, out var parsedVersion, out var parsedBlocked).Should().BeTrue();
        parsedVersion.Should().Be(version);
        parsedBlocked.Should().Be(blocked);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("3")]
    [InlineData("3|2")]
    [InlineData("x|1")]
    public void TryParse_RejectsGarbage(string? value)
    {
        SessionRevocationNames.TryParse(value, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Rejects_WhenBlockedOrVersionDiffers()
    {
        SessionRevocationNames.Rejects(null, 0).Should().BeFalse();
        SessionRevocationNames.Rejects(new SessionRevocationEntry(1, false), 1).Should().BeFalse();
        SessionRevocationNames.Rejects(new SessionRevocationEntry(1, true), 1).Should().BeTrue();
        SessionRevocationNames.Rejects(new SessionRevocationEntry(2, false), 1).Should().BeTrue();
    }

    [Fact]
    public void MissingClaim_ReadsAsZero()
    {
        SessionRevocationNames.ReadVersion(new ClaimsPrincipal(new ClaimsIdentity())).Should().Be(0);
    }

    [Theory]
    [InlineData("/identity/login")]
    [InlineData("/api/v1/auth/refresh")]
    [InlineData("/API/v1/auth/logout")]
    public void CredentialPaths_AreExempt(string path)
    {
        SessionRevocationPaths.IsCredentialExchange(path).Should().BeTrue();
    }

    [Fact]
    public async Task Middleware_BlocksRevokedPrincipal()
    {
        var userId = Guid.NewGuid();
        var store = new StubStore(new SessionRevocationEntry(2, false));
        var nextCalled = false;
        var middleware = new SessionRevocationMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<SessionRevocationMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.User = Principal(userId, "1");

        await middleware.InvokeAsync(context, store);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task Middleware_AllowsMatchingVersionAndLoginPath()
    {
        var userId = Guid.NewGuid();
        var store = new StubStore(new SessionRevocationEntry(1, true));
        var nextCalled = false;
        var middleware = new SessionRevocationMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<SessionRevocationMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/v1/auth/login";
        context.User = Principal(userId, "0");

        await middleware.InvokeAsync(context, store);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Middleware_FailsOpenWhenStoreThrows()
    {
        var nextCalled = false;
        var middleware = new SessionRevocationMiddleware(
            _ =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            },
            NullLogger<SessionRevocationMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.User = Principal(Guid.NewGuid(), "0");

        await middleware.InvokeAsync(context, new ThrowingStore());

        nextCalled.Should().BeTrue();
    }

    private static ClaimsPrincipal Principal(Guid userId, string version)
    {
        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(SessionRevocationNames.ClaimType, version)
            ],
            authenticationType: "Bearer");
        return new ClaimsPrincipal(identity);
    }

    private sealed class StubStore(SessionRevocationEntry entry) : ISessionRevocationStore
    {
        public Task SetAsync(Guid userId, long version, bool blocked, TimeSpan ttl, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SessionRevocationEntry?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<SessionRevocationEntry?>(entry);

        public Task PublishAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ThrowingStore : ISessionRevocationStore
    {
        public Task SetAsync(Guid userId, long version, bool blocked, TimeSpan ttl, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SessionRevocationEntry?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("redis down");

        public Task PublishAsync(Guid userId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
