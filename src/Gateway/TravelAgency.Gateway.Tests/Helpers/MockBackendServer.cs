using System.Net;
using System.Net.Sockets;

namespace TravelAgency.Gateway.Tests.Helpers;

/// <summary>
/// Minimal Kestrel server that echoes the received request path in the response body.
/// Used by YARP routing tests to verify path transforms.
/// </summary>
public sealed class MockBackendServer : IAsyncDisposable
{
    private readonly WebApplication _app;

    private MockBackendServer(WebApplication app)
    {
        _app = app;
    }

    /// <summary>
    /// Base URL of the mock server (e.g. http://127.0.0.1:12345).
    /// </summary>
    public string BaseUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Starts a mock backend on a random port. Returns 200 for any path and echoes
    /// the request path in the response body as JSON: {"path": "/identity/me"}.
    /// </summary>
    /// <param name="authTokenSupport">When true, /identity/login, /identity/register, and /identity/refresh
    /// return 200/201 with token JSON for auth response transform tests; /identity/logout returns 204.</param>
    /// <param name="validJwtFactory">When provided with authTokenSupport, used for accessToken so Gateway
    /// accepts it for protected routes. If null, returns "test-access-token" (invalid for JWT validation).</param>
    public static async Task<MockBackendServer> StartAsync(
        bool authTokenSupport = false,
        Func<string>? validJwtFactory = null,
        CancellationToken cancellationToken = default)
    {
        var port = GetAvailablePort();
        var url = $"http://127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(url);
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        var app = builder.Build();

        app.Run(async context =>
        {
            var path = context.Request.Path.Value ?? "/";
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
                    refreshToken = "test-refresh-token",
                    expiresAt = DateTime.UtcNow.AddHours(1)
                }, cancellationToken);
            }
            else
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { path }, cancellationToken);
            }
        });

        await app.StartAsync(cancellationToken);

        return new MockBackendServer(app) { BaseUrl = url };
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

    private static bool IsLogoutPath(string path) =>
        path.Equals("/identity/logout", StringComparison.OrdinalIgnoreCase);

    private static bool IsAuthTokenPath(string path) =>
        path.Equals("/identity/login", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/identity/register", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/identity/refresh", StringComparison.OrdinalIgnoreCase);

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
