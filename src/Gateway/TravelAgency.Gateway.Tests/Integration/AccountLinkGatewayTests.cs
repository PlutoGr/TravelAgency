using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TravelAgency.Gateway.Tests.Helpers;
using TravelAgency.Shared.Contracts.Auth;
using Xunit;

namespace TravelAgency.Gateway.Tests.Integration;

public class AccountLinkGatewayTests : IAsyncLifetime
{
    private MockBackendServer _mock = null!;
    private GatewayWebApplicationFactory _factory = null!;
    private readonly List<string> _bodies = [];

    public async Task InitializeAsync()
    {
        _mock = await MockBackendServer.StartAsync(authTokenSupport: true, validJwtFactory: () => JwtTokenHelper.GenerateToken());
        _factory = new GatewayWebApplicationFactory(_mock.BaseUrl);
        var tokens = new
        {
            accessToken = JwtTokenHelper.GenerateToken(),
            refreshToken = "settings-refresh",
            expiresAt = MockBackendServer.AuthAccessExpiresAt,
            refreshExpiresAt = MockBackendServer.AuthRefreshExpiresAt
        };
        _mock.Scripts["/identity/me/password"] = async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsJsonAsync(tokens);
        };
        _mock.Scripts["/identity/email-confirmations"] = async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            var body = await reader.ReadToEndAsync();
            _bodies.Add(body);
            context.Response.StatusCode = 200;
            var withTokens = body.Contains("force-tokens", StringComparison.Ordinal);
            object outcome = withTokens
                ? tokens
                : new { outcome = "changed", session = "current" };
            await context.Response.WriteAsJsonAsync(outcome);
        };
        _mock.Scripts["/identity/password-resets/request"] = async context =>
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsJsonAsync(new { detail = "Too many requests." });
        };
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _mock.DisposeAsync();
    }

    [Fact]
    public async Task Tk18_PasswordChangeSetsCookies_ConfirmKeepsOutcome_AndResetRequestBecomesNeutral()
    {
        var client = _factory.CreateClient();
        var password = await client.PostAsJsonAsync("/api/v1/auth/me/password", new { currentPassword = "a", newPassword = "b", passwordConfirm = "b" });
        Assert.Equal(HttpStatusCode.OK, password.StatusCode);
        var passwordBody = await password.Content.ReadAsStringAsync();
        using (var doc = JsonDocument.Parse(passwordBody))
            Assert.True(doc.RootElement.GetProperty("success").GetBoolean());
        Assert.DoesNotContain("accessToken", passwordBody, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(password.Headers.GetValues("Set-Cookie"), header => header.StartsWith("access_token=", StringComparison.Ordinal));

        using var confirmRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/email-confirmations");
        confirmRequest.Headers.Add("Cookie", "refresh_token=from-cookie");
        confirmRequest.Content = JsonContent.Create(new { token = "secret" });
        var confirm = await client.SendAsync(confirmRequest);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);
        var confirmBody = await confirm.Content.ReadAsStringAsync();
        Assert.Contains("changed", confirmBody, StringComparison.Ordinal);
        Assert.DoesNotContain("from-cookie", confirmBody, StringComparison.Ordinal);
        Assert.Contains("from-cookie", _bodies[0], StringComparison.Ordinal);

        var rejected = await client.PostAsJsonAsync("/api/v1/auth/email-confirmations", new { token = "force-tokens" });
        Assert.Equal(HttpStatusCode.BadGateway, rejected.StatusCode);

        var reset = await client.PostAsJsonAsync("/api/v1/auth/password-resets/request", new { email = "a@b.co" });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);
        using var resetJson = JsonDocument.Parse(await reset.Content.ReadAsStringAsync());
        var resetBody = resetJson.RootElement.GetProperty("message").GetString();
        Assert.Equal(AccountLinkMessages.PasswordResetRequested, resetBody);
        Assert.DoesNotContain("Слишком много попыток", resetBody, StringComparison.Ordinal);
        Assert.Equal("no-store", reset.Headers.CacheControl?.ToString());
    }
}
