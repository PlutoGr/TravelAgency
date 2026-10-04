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
    /// REST and gRPC share the public cleartext port (the same 8080 Catalog clients use).
    /// Kestrel itself cannot accept prior-knowledge h2c on an endpoint that also allows
    /// HTTP/1, so HTTP and HTTP/2 each get a loopback-only listener.
    /// <see cref="CleartextH2cProxy"/> owns the public port and splices each connection
    /// to the matching listener. HTTPS, when configured, stays on Kestrel and uses ALPN.
    /// </summary>
    public static void ConfigureHost(IWebHostBuilder host)
    {
        host.ConfigureKestrel((context, options) =>
        {
            foreach (var raw in PublicEndpointUrls.Resolve(context.Configuration))
            {
                var address = BindingAddress.Parse(raw);
                if (!address.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (address.IsUnixPipe || address.IsNamedPipe)
                    continue;

                void UseTls(ListenOptions listen)
                {
                    listen.Protocols = HttpProtocols.Http1AndHttp2;
                    listen.UseHttps();
                }

                if (address.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
                    options.ListenLocalhost(address.Port, UseTls);
                else if (IPAddress.TryParse(address.Host, out var ip))
                    options.Listen(ip, address.Port, UseTls);
                else
                    options.ListenAnyIP(address.Port, UseTls);
            }

            options.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http1);
            options.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http2);
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
        services.AddHostedService<CleartextH2cProxy>();
    }

    public static void ConfigurePipeline(WebApplication app)
    {
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
        app.MapGrpcService<MediaGrpcService>();
        app.MapMediaHealthChecks();
    }
}
