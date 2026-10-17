using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace TravelAgency.Gateway.Tests.Helpers;

/// <summary>
/// Minimal Kestrel server that echoes the received request path in the response body.
/// Used by YARP routing tests to verify path transforms.
/// </summary>
public sealed class MockBackendServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentQueue<string> _requestPaths;
    private readonly ConcurrentQueue<AuthExchange> _authExchanges;

    private MockBackendServer(
        WebApplication app,
        ConcurrentQueue<string> requestPaths,
        ConcurrentQueue<AuthExchange> authExchanges)
    {
        _app = app;
        _requestPaths = requestPaths;
        _authExchanges = authExchanges;
    }

    /// <summary>
    /// Тело запроса, которое мок Identity получил на login, refresh или logout.
    /// </summary>
    public readonly record struct AuthExchange(string Path, string Body);

    /// <summary>
    /// Paths the mock actually received, after YARP rewrote them.
    /// </summary>
    public IReadOnlyList<string> RequestPaths => _requestPaths.ToArray();

    /// <summary>
    /// Тела auth-запросов в порядке получения.
    /// </summary>
    public IReadOnlyList<AuthExchange> AuthExchanges => _authExchanges.ToArray();

    /// <summary>
    /// Base URL of the mock server (e.g. http://127.0.0.1:12345).
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Срок refresh-токена, который мок отдаёт в JSON как refreshExpiresAt.
    /// </summary>
    public static readonly DateTime AuthAccessExpiresAt = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    public static readonly DateTime AuthRefreshExpiresAt = new(2026, 12, 1, 15, 30, 0, DateTimeKind.Utc);

    /// <summary>
    /// Starts a mock backend on a random port. Returns 200 for any path and echoes
    /// the request path in the response body as JSON: {"path": "/identity/me"}.
    /// </summary>
    /// <param name="authTokenSupport">When true, /identity/login, /identity/register, and /identity/refresh
    /// return 200/201 with token JSON for auth response transform tests; /identity/logout returns 204.</param>
    /// <param name="validJwtFactory">When provided with authTokenSupport, used for accessToken so Gateway
    /// accepts it for protected routes. If null, returns "test-access-token" (invalid for JWT validation).</param>
    /// <param name="refreshToken">Refresh token placed in the token JSON. Response.Cookies.Append encodes it.</param>
    /// <param name="rejectMismatchedRefreshToken">When true, /identity/refresh returns 401 unless the body
    /// carries this exact refresh token.</param>
    public static async Task<MockBackendServer> StartAsync(
        bool authTokenSupport = false,
        Func<string>? validJwtFactory = null,
        CancellationToken cancellationToken = default,
        string refreshToken = "test-refresh-token",
        bool rejectMismatchedRefreshToken = false)
    {
        var builder = WebApplication.CreateBuilder();
        // Port 0: the OS assigns the port at bind time, so nothing can take it before Kestrel does.
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        var app = builder.Build();
        var requestPaths = new ConcurrentQueue<string>();
        var authExchanges = new ConcurrentQueue<AuthExchange>();

        app.UseWebSockets();
        app.Run(async context =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                await EchoWebSocketAsync(context);
                return;
            }

            var path = context.Request.Path.Value ?? "/";
            requestPaths.Enqueue(path);
            if (authTokenSupport && (IsLogoutPath(path) || IsAuthTokenPath(path)))
            {
                using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
                var body = await reader.ReadToEndAsync(context.RequestAborted);
                authExchanges.Enqueue(new AuthExchange(path, body));

                if (rejectMismatchedRefreshToken
                    && IsRefreshPath(path)
                    && !BodyHasRefreshToken(body, refreshToken))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(
                        new { message = "Invalid or expired refresh token." },
                        context.RequestAborted);
                    return;
                }
            }

            if (authTokenSupport && IsLogoutPath(path))
            {
                context.Response.StatusCode = 204;
            }
            else if (authTokenSupport && IsAuthTokenPath(path))
            {
                context.Response.StatusCode = path.Contains("register", StringComparison.OrdinalIgnoreCase) ? 201 : 200;
                context.Response.ContentType = "application/json";
                var accessToken = validJwtFactory != null ? validJwtFactory() : "test-access-token";
                await context.Response.WriteAsJsonAsync(new
                {
                    accessToken,
                    refreshToken,
                    expiresAt = AuthAccessExpiresAt,
                    refreshExpiresAt = AuthRefreshExpiresAt
                }, cancellationToken);
            }
            else
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                var routeId = context.Request.Headers[SelectedRouteHeaderTransformProvider.HeaderName].ToString();
                await context.Response.WriteAsJsonAsync(new { path, routeId }, cancellationToken);
            }
        });

        await app.StartAsync(cancellationToken);

        return new MockBackendServer(app, requestPaths, authExchanges) { BaseUrl = app.Urls.First() };
    }

    public async Task StopAsync()
    {
        await _app.StopAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _app.DisposeAsync();
    }

    /// <summary>
    /// Accepts a WebSocket and sends one text frame with the request path so routing tests
    /// can confirm the upgrade was proxied to this backend.
    /// </summary>
    private static async Task EchoWebSocketAsync(HttpContext context)
    {
        using var socket = await context.WebSockets.AcceptWebSocketAsync();
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            path = context.Request.Path.Value ?? "/",
            routeId = context.Request.Headers[SelectedRouteHeaderTransformProvider.HeaderName].ToString()
        }));
        await socket.SendAsync(payload, WebSocketMessageType.Text, endOfMessage: true, context.RequestAborted);

        var buffer = new byte[1024];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var received = await socket.ReceiveAsync(buffer, context.RequestAborted);
                if (received.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", CancellationToken.None);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (WebSocketException)
        {
        }
    }

    private static bool IsLogoutPath(string path) =>
        path.Equals("/identity/logout", StringComparison.OrdinalIgnoreCase);

    private static bool IsRefreshPath(string path) =>
        path.Equals("/identity/refresh", StringComparison.OrdinalIgnoreCase);

    private static bool IsAuthTokenPath(string path) =>
        path.Equals("/identity/login", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/identity/register", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/identity/refresh", StringComparison.OrdinalIgnoreCase);

    private static bool BodyHasRefreshToken(string body, string expected)
    {
        if (string.IsNullOrWhiteSpace(body))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("refreshToken", out var property)
                && property.ValueKind == JsonValueKind.String
                && property.GetString() == expected;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
