using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TravelAgency.Shared.Infrastructure.Hosting;

/// <summary>
/// Configures Kestrel with two cleartext listeners. HTTP/1.1 serves REST and health.
/// HTTP/2 on the gRPC port serves h2c. One socket cannot do both: cleartext has no TLS,
/// so there is no ALPN to negotiate the protocol.
/// </summary>
public static class KestrelServiceListenExtensions
{
    public static WebApplicationBuilder UseServiceListenPorts(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseServiceListenPorts();
        return builder;
    }

    public static IWebHostBuilder UseServiceListenPorts(this IWebHostBuilder host)
    {
        host.ConfigureServices((context, services) =>
        {
            services.AddOptions<ServiceListenSettings>()
                .Bind(context.Configuration.GetSection(ServiceListenSettings.SectionName))
                .PostConfigure(settings =>
                {
                    if (settings.HttpPort <= 0)
                        settings.HttpPort = ServiceListenSettings.DefaultHttpPort;
                    if (settings.GrpcPort <= 0)
                        settings.GrpcPort = ServiceListenSettings.DefaultGrpcPort;
                });
        });

        host.ConfigureKestrel((context, options) =>
        {
            var settings = ServiceListenSettings.FromConfiguration(context.Configuration);
            ConfigureListeners(options, context.Configuration, settings);
        });

        return host;
    }

    public static IApplicationBuilder UseGrpcListenPortGuard(this IApplicationBuilder app) =>
        app.UseMiddleware<GrpcListenPortMiddleware>();

    private static void ConfigureListeners(
        KestrelServerOptions options,
        IConfiguration configuration,
        ServiceListenSettings settings)
    {
        IReadOnlyList<string> urls = PublicEndpointUrls.Resolve(configuration);
        if (urls.Count == 0)
            urls = [$"http://*:{settings.HttpPort}"];

        foreach (var raw in urls)
        {
            var address = BindingAddress.Parse(raw);
            if (address.IsUnixPipe || address.IsNamedPipe)
                throw new InvalidOperationException($"The service listens on TCP. Unsupported URL: {raw}");

            if (address.Port == settings.GrpcPort)
            {
                throw new InvalidOperationException(
                    $"Public URL {raw} uses port {settings.GrpcPort}, which is reserved for gRPC.");
            }

            var https = address.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
            void Configure(ListenOptions listen)
            {
                if (https)
                {
                    listen.Protocols = HttpProtocols.Http1AndHttp2;
                    listen.UseHttps();
                }
                else
                {
                    listen.Protocols = HttpProtocols.Http1;
                }
            }

            if (address.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                || address.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
            {
                options.ListenLocalhost(address.Port, Configure);
            }
            else if (IPAddress.TryParse(address.Host, out var ip))
            {
                options.Listen(ip, address.Port, Configure);
            }
            else
            {
                options.ListenAnyIP(address.Port, Configure);
            }
        }

        options.ListenAnyIP(settings.GrpcPort, listen => listen.Protocols = HttpProtocols.Http2);
    }
}
