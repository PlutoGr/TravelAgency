using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Serilog;
using TravelAgency.Media.API.Extensions;
using TravelAgency.Media.API.Hosting;
using TravelAgency.Media.API.Middleware;
using TravelAgency.Media.Infrastructure;
using TravelAgency.Media.Infrastructure.Extensions;
using TravelAgency.Media.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddMediaSerilog();
Program.ConfigureHost(builder.WebHost);
Program.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

Program.ConfigurePipeline(app);

app.Run();

public partial class Program
{
    /// <summary>
    /// Public HTTP (REST, health) stays on the configured URL, HTTP/1.1.
    /// gRPC is a second listener: port <see cref="MediaPorts.Grpc"/>, HTTP/2 only.
    /// Cleartext cannot negotiate HTTP/1 and HTTP/2 on one socket (no ALPN), so h2c
    /// gets its own port. HTTPS, when configured, stays on Kestrel and uses ALPN.
    /// </summary>
    public static void ConfigureHost(IWebHostBuilder host)
    {
        host.ConfigureKestrel((context, options) =>
        {
            var urls = PublicEndpointUrls.Resolve(context.Configuration);
            if (urls.Count == 0)
                urls = ["http://*:8080"];

            foreach (var raw in urls)
            {
                var address = BindingAddress.Parse(raw);
                if (address.IsUnixPipe || address.IsNamedPipe)
                    throw new InvalidOperationException($"Media listens on TCP. Unsupported URL: {raw}");

                if (address.Port == MediaPorts.Grpc)
                {
                    throw new InvalidOperationException(
                        $"Public URL {raw} uses port {MediaPorts.Grpc}, which is reserved for gRPC.");
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

            options.ListenAnyIP(MediaPorts.Grpc, listen => listen.Protocols = HttpProtocols.Http2);
        });
    }

    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IExceptionMapper, MediaExceptionMapper>();
        services.AddControllers()
            .AddApplicationPart(typeof(TravelAgency.Media.API.Controllers.MediaController).Assembly);
        services.AddMediaAuthentication(configuration);
        services.AddMediaAuthorization();
        services.AddMediaInfrastructure(configuration);
        services.AddMediaCors(configuration);
        services.AddMediaHealthChecks();
        services.AddMediaSwagger();
        services.AddMediaTracing();
        services.AddSingleton<GrpcAuthInterceptor>();
        services.AddGrpc(options => options.Interceptors.Add<GrpcAuthInterceptor>());
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseMiddleware<GrpcListenPortMiddleware>();
        app.UseMediaCors();

        app.UseSerilogRequestLogging();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseMediaSwagger();
        }

        app.UseMediaMigrations();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapGrpcService<MediaGrpcService>().RequireHost(MediaPorts.GrpcHostPattern);
        app.MapMediaHealthChecks();
    }
}
