using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace TravelAgency.Gateway.Tests.Integration;

/// <summary>
/// Set-Cookie stores the refresh token percent-encoded. The browser sends that
/// value back unchanged. Identity must receive the original token.
/// </summary>
public class RefreshCookieRoundTripTests : IAsyncLifetime
{
    private const string Token = "ab+cd/ef==";

    private Helpers.MockBackendServer _mock = null!;
    private Helpers.GatewayWebApplicationFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _mock = await Helpers.MockBackendServer.StartAsync(
            authTokenSupport: true,
            validJwtFactory: () => Helpers.JwtTokenHelper.GenerateToken(),
            refreshToken: Token,
            rejectMismatchedRefreshToken: true);
        _factory = new Helpers.GatewayWebApplicationFactory(_mock.BaseUrl);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _mock.DisposeAsync();
    }

    [Fact]
    public async Task LoginSetCookie_SentUnchanged_RefreshAndLogoutDeliverOriginalToken()
    {
        using var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "x" });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var raw = ReadSetCookieValue(login, "refresh_token");
        Assert.Equal(Uri.EscapeDataString(Token), raw);
        Assert.Contains("%2B", raw, StringComparison.Ordinal);
        Assert.Contains("%2F", raw, StringComparison.Ordinal);
        Assert.Contains("%3D", raw, StringComparison.Ordinal);

        var refresh = await PostWithRawCookie(client, "/api/v1/auth/refresh", raw);
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        Assert.Equal(Token, RefreshTokenSentToIdentity("/identity/refresh"));

        var logout = await PostWithRawCookie(client, "/api/v1/auth/logout", raw);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(Token, RefreshTokenSentToIdentity("/identity/logout"));
    }

    [Theory]
    [InlineData("%")]
    [InlineData("%2")]
    [InlineData("%ZZ")]
    [InlineData("%2G")]
    [InlineData("ab%")]
    [InlineData("%E0%A4%A")]
    public async Task Refresh_WhenPercentEncodingIsBroken_DoesNotReturn500(string raw)
    {
        using var client = CreateClient();

        var response = await PostWithRawCookie(client, "/api/v1/auth/refresh", raw);

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WhenPercentEncodingIsBroken_DoesNotReturn500()
    {
        using var client = CreateClient();

        var response = await PostWithRawCookie(client, "/api/v1/auth/logout", "%ZZ");

        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    private HttpClient CreateClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static async Task<HttpResponseMessage> PostWithRawCookie(HttpClient client, string path, string rawCookieValue)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.TryAddWithoutValidation("Cookie", "refresh_token=" + rawCookieValue);
        request.Content = new ByteArrayContent([]);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return await client.SendAsync(request);
    }

    private static string ReadSetCookieValue(HttpResponseMessage response, string name)
    {
        var prefix = name + "=";
        foreach (var header in response.Headers.GetValues("Set-Cookie"))
        {
            if (!header.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var end = header.IndexOf(';');
            var value = end < 0 ? header[prefix.Length..] : header[prefix.Length..end];
            if (value.Length == 0)
                continue;

            return value;
        }

        throw new InvalidOperationException($"Set-Cookie {name} was not set.");
    }

    private string RefreshTokenSentToIdentity(string path)
    {
        var exchange = _mock.AuthExchanges.Last(item =>
            item.Path.Equals(path, StringComparison.OrdinalIgnoreCase));
        using var doc = JsonDocument.Parse(exchange.Body);
        return doc.RootElement.GetProperty("refreshToken").GetString()
            ?? throw new InvalidOperationException("refreshToken was empty.");
    }
}
