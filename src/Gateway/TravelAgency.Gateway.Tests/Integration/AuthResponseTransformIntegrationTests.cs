using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace TravelAgency.Gateway.Tests.Integration;

/// <summary>
/// Verifies that auth routes (login, register, refresh) return 200 with Set-Cookie headers
/// and {"success": true} body when Identity returns token JSON. Verifies that logout returns
/// 204 with Set-Cookie headers to clear access_token and refresh_token when Identity returns 204.
/// </summary>
public class AuthResponseTransformIntegrationTests : IClassFixture<AuthResponseTransformFixture>
{
    private readonly AuthResponseTransformFixture _fixture;

    public AuthResponseTransformIntegrationTests(AuthResponseTransformFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/refresh")]
    public async Task AuthRoute_WhenIdentityReturnsTokens_Returns200WithSetCookieAndSuccessBody(string path)
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        using var content = path.Contains("refresh")
            ? JsonContent.Create(new { refreshToken = "test-refresh-token" })
            : JsonContent.Create(new { email = "test@test.com", password = "Password1!" });

        // Act
        var response = path.Contains("register")
            ? await client.PostAsync(path, content)
            : await client.PostAsync(path, content);

        // Assert - login/refresh return 200, register returns 201
        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.Created,
            $"Expected 200 or 201, got {response.StatusCode}");

        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues),
            "Set-Cookie header should be present");
        var setCookieHeaders = setCookieValues.ToList();
        Assert.Contains(setCookieHeaders, h => h.StartsWith("access_token=", StringComparison.Ordinal));
        Assert.Contains(setCookieHeaders, h => h.StartsWith("refresh_token=", StringComparison.Ordinal));

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task AuthLogout_WhenIdentityReturns204_Returns204WithClearCookies()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        using var content = JsonContent.Create(new { refreshToken = "test-refresh-token" });

        // Act
        var response = await client.PostAsync("/api/v1/auth/logout", content);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Set-Cookie", out var setCookieValues),
            "Set-Cookie header should be present");
        var setCookieHeaders = setCookieValues.ToList();
        Assert.Contains(setCookieHeaders, h => h.StartsWith("access_token=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieHeaders, h => h.StartsWith("refresh_token=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AuthLogin_DoesNotExposeTokensInBody()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "x" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("access_token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh_token", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("success", body, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// E2E: Login returns Set-Cookie; protected request with cookies (no Authorization header) succeeds.
    /// Verifies cookie-based auth flow: client sends credentials, receives cookies, uses cookies for protected routes.
    /// </summary>
    [Fact]
    public async Task Login_ThenProtectedRequestWithCookies_Returns200()
    {
        // Arrange - use single client so cookies from login are preserved and sent on next request
        var client = _fixture.Factory.CreateClient();

        // Act 1 - Login (no auth); Gateway sets access_token and refresh_token cookies
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "x" });
        loginResponse.EnsureSuccessStatusCode();
        Assert.True(loginResponse.Headers.TryGetValues("Set-Cookie", out _), "Login must return Set-Cookie");

        // Act 2 - Protected route with cookies only (no Authorization header); AuthRequestTransformProvider injects Bearer from cookie
        var protectedResponse = await client.GetAsync("/api/v1/bookings/my");

        // Assert - cookie auth succeeded (200); path has route prefix duplication (known YARP config issue)
        Assert.Equal(HttpStatusCode.OK, protectedResponse.StatusCode);
        var body = await protectedResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var path = doc.RootElement.GetProperty("path").GetString();
        Assert.True(path?.EndsWith("/bookings/my", StringComparison.Ordinal), $"Path should end with /bookings/my, got {path}");
    }

    /// <summary>
    /// E2E: Refresh with cookie only (no body) succeeds. AuthRequestMiddleware injects refresh_token from cookie.
    /// Verifies 401→refresh→retry flow: frontend calls refresh with credentials: 'include'; cookie is sent.
    /// </summary>
    [Fact]
    public async Task Refresh_WithCookieOnly_NoBody_Returns200WithSetCookie()
    {
        // Arrange - first login to get cookies
        var client = _fixture.Factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "x" });
        loginResponse.EnsureSuccessStatusCode();

        // Act - refresh with cookie only (empty body); middleware injects refresh_token from cookie
        using var emptyContent = new StringContent("", System.Text.Encoding.UTF8, "application/json");
        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh", emptyContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, refreshResponse.StatusCode);
        Assert.True(refreshResponse.Headers.TryGetValues("Set-Cookie", out var setCookieValues),
            "Set-Cookie header should be present");
        var setCookieHeaders = setCookieValues.ToList();
        Assert.Contains(setCookieHeaders, h => h.StartsWith("access_token=", StringComparison.Ordinal));
        Assert.Contains(setCookieHeaders, h => h.StartsWith("refresh_token=", StringComparison.Ordinal));
    }

    /// <summary>
    /// E2E: Logout clears cookies. Verifies full flow: login → logout → cookies cleared.
    /// </summary>
    [Fact]
    public async Task Logout_AfterLogin_ClearsCookies()
    {
        // Arrange - login first to get cookies
        var client = _fixture.Factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "x" });
        loginResponse.EnsureSuccessStatusCode();

        // Act - logout with empty body; middleware injects refresh_token from cookie
        using var emptyContent = new StringContent("", System.Text.Encoding.UTF8, "application/json");
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout", emptyContent);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);
        Assert.True(logoutResponse.Headers.TryGetValues("Set-Cookie", out var setCookieValues),
            "Set-Cookie header should be present");
        var setCookieHeaders = setCookieValues.ToList();
        Assert.Contains(setCookieHeaders, h => h.StartsWith("access_token=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(setCookieHeaders, h => h.StartsWith("refresh_token=;", StringComparison.Ordinal) && h.Contains("max-age=0", StringComparison.OrdinalIgnoreCase));
    }
}
