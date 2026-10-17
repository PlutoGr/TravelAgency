using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Shared.Infrastructure.Hosting;

namespace TravelAgency.Testing;

/// <summary>
/// Test listeners for real Kestrel: REST on HTTP/1.1 and gRPC on HTTP/2, both on 127.0.0.1:0.
/// The OS picks each port at bind time and the socket stays bound from then on, so nothing
/// (Docker, Testcontainers, a parallel test) can take the port in between.
/// <para>
/// REST: Kestrel binds 127.0.0.1:0 itself; the port is read from <see cref="ListenOptions"/> after start.
/// </para>
/// <para>
/// gRPC: production splits traffic by the configured gRPC port, and the gRPC host rule
/// (<c>RequireHost("*:port")</c>) is fixed when endpoints are built, before Kestrel binds.
/// So the gRPC socket is bound to 127.0.0.1:0 here, kept open, and handed to Kestrel by handle.
/// The port is known before the app is built and is never released.
/// </para>
/// </summary>
internal sealed class KestrelLoopbackListeners : IDisposable
{
    private readonly Socket _grpcSocket;
    private ListenOptions? _rest;

    public KestrelLoopbackListeners()
    {
        _grpcSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _grpcSocket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        _grpcSocket.Listen();
        GrpcPort = ((IPEndPoint)_grpcSocket.LocalEndPoint!).Port;
    }

    public int GrpcPort { get; }

    public string GrpcAddress => $"http://127.0.0.1:{GrpcPort}";

    public int RestPort
    {
        get
        {
            var port = _rest?.IPEndPoint?.Port ?? 0;
            if (port == 0)
                throw new InvalidOperationException("The REST listener is not bound yet. Read it after StartAsync.");
            return port;
        }
    }

    public string RestAddress => $"http://127.0.0.1:{RestPort}";

    /// <summary>Use instead of <c>UseServiceListenPorts</c>, which binds fixed ports from configuration.</summary>
    public void Configure(IWebHostBuilder host)
    {
        host.ConfigureServices(services =>
            services.Configure<ServiceListenSettings>(settings => settings.GrpcPort = GrpcPort));
        host.ConfigureKestrel(options =>
        {
            options.Listen(IPAddress.Loopback, 0, listen =>
            {
                listen.Protocols = HttpProtocols.Http1;
                _rest = listen;
            });
            options.ListenHandle(
                (ulong)_grpcSocket.SafeHandle.DangerousGetHandle(),
                listen => listen.Protocols = HttpProtocols.Http2);
        });
    }

    /// <summary>Kestrel does not own the gRPC handle; close it after the app has stopped.</summary>
    public void Dispose() => _grpcSocket.Dispose();
}
