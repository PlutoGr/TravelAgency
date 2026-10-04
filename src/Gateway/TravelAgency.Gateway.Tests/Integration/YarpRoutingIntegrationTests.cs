using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Xunit;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Gateway.Tests.Integration;

/// <summary>
/// Verifies that YARP correctly routes and transforms paths for anonymous and
/// authenticated routes. Uses a mock HTTP backend that echoes the received path.
/// </summary>
public class YarpRoutingIntegrationTests : IClassFixture<YarpRoutingFixture>
{
    private readonly YarpRoutingFixture _fixture;

    public YarpRoutingIntegrationTests(YarpRoutingFixture fixture)
    {
        _fixture = fixture;
    }

    private HttpClient CreateClient(bool withAuth = false)
    {
        var client = _fixture.Factory.CreateClient();
        if (withAuth)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client));
        }
        return client;
    }

    private static async Task<string> GetEchoedPathAsync(HttpResponseMessage response)
    {
        var echo = await ReadEchoAsync(response);
        return echo.Path;
    }

    private static async Task<(string Path, string RouteId)> ReadEchoAsync(HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var path = doc.RootElement.GetProperty("path").GetString() ?? string.Empty;
        var routeId = doc.RootElement.TryGetProperty("routeId", out var route)
            ? route.GetString() ?? string.Empty
            : string.Empty;
        return (path, routeId);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string url, string? accessTokenCookie = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrEmpty(accessTokenCookie))
        {
            request.Headers.TryAddWithoutValidation("Cookie", $"access_token={accessTokenCookie}");
        }

        if (method == HttpMethod.Post)
        {
            request.Content = new StringContent(string.Empty);
        }

        return request;
    }

    [Fact]
    public async Task Auth_GetMe_ProxiesToIdentityMe()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/api/v1/auth/me");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/identity/me", path);
    }

    [Fact]
    public async Task Auth_PostLogin_ProxiesToIdentityLogin()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "test@test.com", password = "x" });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/identity/login", path);
    }

    [Fact]
    public async Task Catalog_GetTours_ProxiesToCatalogTours()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/api/v1/catalog/tours");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/catalog/tours", path);
    }

    [Fact]
    public async Task Bookings_GetMy_WithJwt_ProxiesToBookingsMy()
    {
        // Arrange
        var client = CreateClient(withAuth: true);

        // Act
        var response = await client.GetAsync("/api/v1/bookings/my");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/bookings/my", path);
    }

    [Fact]
    public async Task Bookings_GetMy_WithoutJwt_Returns401()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/api/v1/bookings/my");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Favorites_Get_WithJwt_ProxiesToFavorites()
    {
        // Arrange
        var client = CreateClient(withAuth: true);

        // Act
        var response = await client.GetAsync("/api/v1/favorites");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/favorites", path);
    }

    [Fact]
    public async Task Chat_PostHubNegotiate_WithAccessTokenCookie_SelectsChatHubRoute()
    {
        // Arrange — dev nginx strips Authorization; the JWT arrives as the access_token cookie.
        var client = CreateClient(withAuth: false);
        var token = Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client);
        var request = CreateRequest(HttpMethod.Post, "/api/v1/chat/hubs/chat/negotiate?negotiateVersion=1", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = await ReadEchoAsync(response);
        Assert.Equal("chat-hub-route", echo.RouteId);
        Assert.Equal("/hubs/chat/negotiate", echo.Path);
    }

    [Fact]
    public async Task Chat_PostHubNegotiate_WithAccessTokenQuery_SelectsChatHubRoute()
    {
        // Arrange — SignalR WebSocket clients pass the JWT as the access_token query value.
        var client = CreateClient(withAuth: false);
        var token = Uri.EscapeDataString(Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client));

        // Act
        var response = await client.SendAsync(CreateRequest(
            HttpMethod.Post,
            $"/api/v1/chat/hubs/chat/negotiate?negotiateVersion=1&access_token={token}"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = await ReadEchoAsync(response);
        Assert.Equal("chat-hub-route", echo.RouteId);
        Assert.Equal("/hubs/chat/negotiate", echo.Path);
    }

    [Fact]
    public async Task Chat_PostHubNegotiate_WithoutAuth_Returns401()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.SendAsync(CreateRequest(HttpMethod.Post, "/api/v1/chat/hubs/chat/negotiate"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_GetBookingMessages_WithAccessTokenQueryOnly_Returns401()
    {
        // Arrange — a query token must not authenticate chat REST. URLs are logged and stored in history.
        var client = CreateClient(withAuth: false);
        var token = Uri.EscapeDataString(Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client));
        var bookingId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/chat/booking/{bookingId}/messages?access_token={token}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_GetHubsLookalike_WithAccessTokenQueryOnly_Returns401()
    {
        // Arrange — "/api/v1/chat/hubsX" shares a string prefix with the hub but is a different segment.
        var client = CreateClient(withAuth: false);
        var token = Uri.EscapeDataString(Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client));

        // Act
        var response = await client.GetAsync($"/api/v1/chat/hubsX?access_token={token}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Bookings_Get_WithAccessTokenQueryOnly_Returns401()
    {
        // Arrange — neighbouring routes stay closed to a token parked in the query string.
        var client = CreateClient(withAuth: false);
        var token = Uri.EscapeDataString(Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client));

        // Act
        var response = await client.GetAsync($"/api/v1/bookings?access_token={token}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Chat_DeleteHub_WithAccessTokenCookie_SelectsChatHubRoute()
    {
        // Arrange — SignalR long polling sends DELETE /hubs/chat?id=... when the connection stops.
        var client = CreateClient(withAuth: false);
        var token = Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client);
        var request = CreateRequest(HttpMethod.Delete, "/api/v1/chat/hubs/chat?id=x", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = await ReadEchoAsync(response);
        Assert.Equal("chat-hub-route", echo.RouteId);
        Assert.Equal("/hubs/chat", echo.Path);
    }

    [Fact]
    public async Task Chat_GetBookingMessages_WithAccessTokenCookie_SelectsChatRoute()
    {
        // Arrange — REST must stay on chat-route (prefix /api/v1 only), even though hub Order is lower.
        var client = CreateClient(withAuth: false);
        var token = Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client);
        var bookingId = Guid.NewGuid();
        var request = CreateRequest(HttpMethod.Get, $"/api/v1/chat/booking/{bookingId}/messages", token);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var echo = await ReadEchoAsync(response);
        Assert.Equal("chat-route", echo.RouteId);
        Assert.Equal($"/chat/booking/{bookingId}/messages", echo.Path);
    }

    [Fact(Timeout = 15000)]
    public async Task Chat_WebSocket_WithAccessTokenCookie_ProxiesToHubsChat()
    {
        // Arrange — same-origin browser sends the access_token cookie on the WebSocket handshake.
        var token = Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client);

        // Act
        var echo = await ConnectHubWebSocketAsync(socket =>
        {
            var http = _fixture.Factory.CreateClient();
            socket.Options.Cookies = new CookieContainer();
            socket.Options.Cookies.Add(http.BaseAddress!, new Cookie("access_token", token));
        }, "id=connection-1");

        // Assert — upgrade reached chat-service at the hub path, no HTTP fallback.
        Assert.Equal("chat-hub-route", echo.RouteId);
        Assert.Equal("/hubs/chat", echo.Path);
    }

    [Fact(Timeout = 15000)]
    public async Task Chat_WebSocket_WithAccessTokenQuery_ProxiesToHubsChat()
    {
        // Arrange — SignalR puts the JWT in the access_token query when the handshake cannot set Authorization.
        var token = Uri.EscapeDataString(Helpers.JwtTokenHelper.GenerateToken(role: AppRoles.Client));

        // Act
        var echo = await ConnectHubWebSocketAsync(_ => { }, $"id=connection-1&access_token={token}");

        // Assert
        Assert.Equal("chat-hub-route", echo.RouteId);
        Assert.Equal("/hubs/chat", echo.Path);
    }

    [Fact(Timeout = 15000)]
    public async Task Chat_WebSocket_WithoutAuth_IsRejected()
    {
        using var http = _fixture.Factory.CreateClient();
        var uri = new UriBuilder(http.BaseAddress!)
        {
            Scheme = "ws",
            Path = "/api/v1/chat/hubs/chat"
        }.Uri;

        using var socket = new ClientWebSocket();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        var ex = await Assert.ThrowsAnyAsync<WebSocketException>(() => socket.ConnectAsync(uri, cts.Token));
        Assert.Contains("401", ex.Message, StringComparison.Ordinal);
    }

    private async Task<(string Path, string RouteId)> ConnectHubWebSocketAsync(
        Action<ClientWebSocket> configure,
        string query)
    {
        using var http = _fixture.Factory.CreateClient();
        var uri = new UriBuilder(http.BaseAddress!)
        {
            Scheme = "ws",
            Path = "/api/v1/chat/hubs/chat",
            Query = query
        }.Uri;

        using var socket = new ClientWebSocket();
        configure(socket);
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await socket.ConnectAsync(uri, cts.Token);

        var buffer = new byte[512];
        var received = await socket.ReceiveAsync(buffer, cts.Token);
        var json = Encoding.UTF8.GetString(buffer, 0, received.Count);
        using var doc = JsonDocument.Parse(json);

        Assert.Equal(WebSocketState.Open, socket.State);
        var path = doc.RootElement.GetProperty("path").GetString() ?? string.Empty;
        var routeId = doc.RootElement.GetProperty("routeId").GetString() ?? string.Empty;

        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", cts.Token);
        return (path, routeId);
    }

    [Fact]
    public async Task Media_GetPresign_WithJwt_ProxiesToMediaPresign()
    {
        // Arrange
        var client = CreateClient(withAuth: true);

        // Act
        var response = await client.GetAsync("/api/v1/media/presign");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var path = await GetEchoedPathAsync(response);
        Assert.Equal("/media/presign", path);
    }

    [Fact]
    public async Task HealthReady_Returns200_WithMockBackend()
    {
        // Arrange
        var client = CreateClient(withAuth: false);

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
