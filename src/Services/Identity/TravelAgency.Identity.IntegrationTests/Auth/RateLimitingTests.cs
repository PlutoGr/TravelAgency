using System.Net;
using System.Net.Http.Json;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Auth.Commands.Logout;

namespace TravelAgency.Identity.IntegrationTests.Auth;

/// <summary>
/// Covers FIX-002: The "auth" rate limiting policy (PermitLimit=5 per 60 s sliding window)
/// is applied to the /login, /register, and /refresh endpoints.
/// Endpoints that are intentionally exempt (e.g. /logout) must never return 429.
///
/// Each test creates its own <see cref="CustomWebApplicationFactory"/> so that the in-process
/// rate-limiter state is fully isolated between tests.
/// </summary>
public class RateLimitingTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RateLimitingTests()
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_WhenFifthRequestSucceeds_SixthRequestReturns429()
    {
        var request = new LoginRequest("noexist@example.com", "Password1!");

        for (var i = 0; i < 5; i++)
        {
            // Each request passes the rate limiter (may return 401, that is expected).
            await _client.PostAsJsonAsync("/identity/login", request);
        }

        var response = await _client.PostAsJsonAsync("/identity/login", request);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Register_WhenFifthRequestSucceeds_SixthRequestReturns429()
    {
        // The first register for this email succeeds (201); subsequent ones return 409.
        // All five pass the rate limiter regardless of their response status.
        for (var i = 0; i < 5; i++)
        {
            var request = new RegisterRequest($"ratelimit{i}@example.com", "Password1!", "Test", "User", null);
            await _client.PostAsJsonAsync("/identity/register", request);
        }

        var sixthRequest = new RegisterRequest("ratelimit5@example.com", "Password1!", "Test", "User", null);
        var response = await _client.PostAsJsonAsync("/identity/register", sixthRequest);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Refresh_WhenFifthRequestSucceeds_SixthRequestReturns429()
    {
        var request = new RefreshTokenRequest("dummy");

        for (var i = 0; i < 5; i++)
        {
            // Each request passes the rate limiter (may return 401, that is expected).
            await _client.PostAsJsonAsync("/identity/refresh", request);
        }

        var response = await _client.PostAsJsonAsync("/identity/refresh", request);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Logout_IsNotRateLimited_Returns_NotTooManyRequests()
    {
        var request = new LogoutCommand("dummy");

        for (var i = 0; i < 7; i++)
        {
            var response = await _client.PostAsJsonAsync("/identity/logout", request);

            // Logout is exempt from the "auth" rate-limit policy;
            // any status other than 429 is acceptable.
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }
}
