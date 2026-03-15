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
    public static async Task<MockBackendServer> StartAsync(CancellationToken cancellationToken = default)
    {
        var port = GetAvailablePort();
        var url = $"http://127.0.0.1:{port}";

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls(url);
        builder.WebHost.ConfigureLogging(l =>
        {
            l.ClearProviders();
            l.SetMinimumLevel(LogLevel.Warning);
        });

        var app = builder.Build();

        app.Use(async (context, next) =>
        {
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            var path = context.Request.Path.Value ?? "/";
            await context.Response.WriteAsJsonAsync(new { path }, cancellationToken);
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

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
