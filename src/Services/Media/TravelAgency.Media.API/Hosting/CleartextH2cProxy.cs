using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;

namespace TravelAgency.Media.API.Hosting;

/// <summary>
/// Shares one cleartext public port between HTTP/1.1 REST and HTTP/2 gRPC.
/// Kestrel 10 selects HTTP/1 whenever a cleartext endpoint allows both protocols
/// (there is no ALPN) and replies to the HTTP/2 client preface with GOAWAY
/// HTTP_1_1_REQUIRED. TLS endpoints stay on Kestrel, which negotiates via ALPN.
/// The proxy peeks at most 24 bytes, then splices the socket to a loopback
/// listener that speaks only the chosen protocol. The storage bucket and gRPC
/// are never opened here.
/// </summary>
internal sealed class CleartextH2cProxy(
    IServer server,
    IConfiguration configuration,
    ILogger<CleartextH2cProxy> logger) : IHostedLifecycleService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan PrefaceTimeout = TimeSpan.FromSeconds(10);

    private readonly List<TcpListener> _listeners = [];
    private readonly ConcurrentDictionary<TcpClient, byte> _clients = new();
    private CancellationTokenSource? _shutdown;

    public Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StartedAsync(CancellationToken cancellationToken)
    {
        if (!IsKestrel(server))
            return;

        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null || addresses.Count == 0)
            return;

        var upstream = await ResolveUpstreamAsync(addresses, cancellationToken);
        var cleartext = PublicEndpointUrls.Resolve(configuration)
            .Select(BindingAddress.Parse)
            .Where(static address => address.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (cleartext.Length == 0)
        {
            logger.LogWarning(
                "Cleartext h2c proxy has HTTP listeners on loopback but no public http URL. REST and gRPC stay on the loopback ports Kestrel chose.");
            return;
        }

        foreach (var address in cleartext)
            BindPublic(address);

        _shutdown = new CancellationTokenSource();
        foreach (var listener in _listeners)
            _ = AcceptLoopAsync(listener, upstream, _shutdown.Token);

        logger.LogInformation(
            "Cleartext h2c proxy is listening. HTTP/1 upstream 127.0.0.1:{Http1Port}, HTTP/2 upstream 127.0.0.1:{Http2Port}.",
            upstream.Http1Port,
            upstream.Http2Port);
    }

    public Task StoppingAsync(CancellationToken cancellationToken)
    {
        Stop();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task StoppedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Stop()
    {
        _shutdown?.Cancel();
        foreach (var listener in _listeners)
            listener.Dispose();

        foreach (var client in _clients.Keys)
            client.Dispose();

        _clients.Clear();
        _listeners.Clear();
    }

    private async Task<Upstream> ResolveUpstreamAsync(
        ICollection<string> addresses,
        CancellationToken cancellationToken)
    {
        var loopback = addresses
            .Select(BindingAddress.Parse)
            .Where(static address =>
                address.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                && IsLoopback(address.Host))
            .ToArray();

        var probes = await Task.WhenAll(loopback.Select(async address =>
        {
            var speaksHttp1 = await ProbeHttp1Async(address, cancellationToken);
            return (address, speaksHttp1);
        }));

        BindingAddress? http1 = null;
        BindingAddress? http2 = null;
        foreach (var probe in probes)
        {
            if (probe.speaksHttp1)
                http1 ??= probe.address;
            else
                http2 ??= probe.address;
        }

        if (http1 is null || http2 is null || http1.Port == http2.Port)
        {
            var described = string.Join(
                ", ",
                probes.Select(probe => $"{probe.address.Host}:{probe.address.Port}={(probe.speaksHttp1 ? "http1" : "not-http1")}"));
            throw new InvalidOperationException(
                "Cleartext h2c proxy could not tell the loopback HTTP/1 and HTTP/2 listeners apart. " + described);
        }

        return new Upstream(http1.Port, http2.Port);
    }

    private static async Task<bool> ProbeHttp1Async(BindingAddress address, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new TcpClient();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(ProbeTimeout);
            await client.ConnectAsync(LoopbackAddress(address.Host), address.Port, timeout.Token);
            client.NoDelay = true;
            var stream = client.GetStream();
            var request = "GET /health/live HTTP/1.1\r\nHost: 127.0.0.1\r\nConnection: close\r\n\r\n"u8.ToArray();
            await stream.WriteAsync(request, timeout.Token);
            var buffer = new byte[64];
            var read = await stream.ReadAsync(buffer, timeout.Token);
            return read >= 12 && buffer.AsSpan(0, 12).SequenceEqual("HTTP/1.1 200"u8);
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException or TimeoutException)
        {
            return false;
        }
    }

    private void BindPublic(BindingAddress address)
    {
        if (address.IsUnixPipe || address.IsNamedPipe)
            throw new InvalidOperationException("Cleartext h2c proxy supports TCP URLs only.");

        if (address.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || address.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            Listen(IPAddress.Loopback, address.Port);
            TryListen(IPAddress.IPv6Loopback, address.Port);
            return;
        }

        if (IPAddress.TryParse(address.Host, out var ip))
        {
            Listen(ip, address.Port);
            return;
        }

        Listen(IPAddress.Any, address.Port);
        TryListen(IPAddress.IPv6Any, address.Port);
    }

    private void Listen(IPAddress address, int port)
    {
        var listener = new TcpListener(address, port);
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
            listener.Server.DualMode = false;

        listener.Server.NoDelay = true;
        listener.Start();
        _listeners.Add(listener);
        logger.LogInformation("Cleartext h2c proxy bound {Address}:{Port}.", address, port);
    }

    private void TryListen(IPAddress address, int port)
    {
        try
        {
            Listen(address, port);
        }
        catch (Exception ex) when (ex is SocketException or InvalidOperationException)
        {
            logger.LogDebug(ex, "Cleartext h2c proxy skipped {Address}:{Port}.", address, port);
        }
    }

    private async Task AcceptLoopAsync(TcpListener listener, Upstream upstream, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(cancellationToken);
                _clients.TryAdd(client, 0);
                _ = ForwardAsync(client, upstream, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is OperationCanceledException or ObjectDisposedException or SocketException)
        {
        }
    }

    private async Task ForwardAsync(TcpClient client, Upstream upstream, CancellationToken cancellationToken)
    {
        TcpClient? upstreamClient = null;
        try
        {
            client.NoDelay = true;
            var clientStream = client.GetStream();
            var (isHttp2, buffered, count) = await ReadPrefaceAsync(clientStream, cancellationToken);
            if (count == 0)
                return;

            upstreamClient = new TcpClient { NoDelay = true };
            _clients.TryAdd(upstreamClient, 0);
            await upstreamClient.ConnectAsync(
                IPAddress.Loopback,
                isHttp2 ? upstream.Http2Port : upstream.Http1Port,
                cancellationToken);

            var upstreamStream = upstreamClient.GetStream();
            await upstreamStream.WriteAsync(buffered.AsMemory(0, count), cancellationToken);

            var clientToUpstream = CopyAsync(clientStream, upstreamStream, upstreamClient.Client, cancellationToken);
            var upstreamToClient = CopyAsync(upstreamStream, clientStream, client.Client, cancellationToken);
            await Task.WhenAll(clientToUpstream, upstreamToClient);
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException)
        {
        }
        finally
        {
            client.Dispose();
            _clients.TryRemove(client, out _);
            if (upstreamClient is not null)
            {
                upstreamClient.Dispose();
                _clients.TryRemove(upstreamClient, out _);
            }
        }
    }

    private static async Task<(bool IsHttp2, byte[] Buffered, int Count)> ReadPrefaceAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[Http2ConnectionPreface.Bytes.Length];
        var count = 0;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(PrefaceTimeout);

        while (count < buffer.Length)
        {
            int read;
            try
            {
                read = await stream.ReadAsync(buffer.AsMemory(count, buffer.Length - count), timeout.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return (false, buffer, count);
            }

            if (read == 0)
                return (false, buffer, count);

            count += read;
            if (!Http2ConnectionPreface.IsPrefix(buffer.AsSpan(0, count)))
                return (false, buffer, count);
        }

        return (Http2ConnectionPreface.IsComplete(buffer), buffer, count);
    }

    private static async Task CopyAsync(
        NetworkStream from,
        NetworkStream to,
        Socket destination,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[64 * 1024];
        try
        {
            while (true)
            {
                var read = await from.ReadAsync(buffer, cancellationToken);
                if (read == 0)
                    break;

                await to.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or OperationCanceledException)
        {
            return;
        }

        try
        {
            destination.Shutdown(SocketShutdown.Send);
        }
        catch (SocketException)
        {
        }
    }

    private static bool IsKestrel(IServer server) => server.GetType().Name == "KestrelServerImpl";

    private static bool IsLoopback(string host) =>
        host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
        || host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase)
        || host.Equals("[::1]", StringComparison.OrdinalIgnoreCase)
        || host.Equals("::1", StringComparison.OrdinalIgnoreCase);

    private static IPAddress LoopbackAddress(string host) =>
        host.Contains(':') ? IPAddress.IPv6Loopback : IPAddress.Loopback;

    private readonly record struct Upstream(int Http1Port, int Http2Port);
}
