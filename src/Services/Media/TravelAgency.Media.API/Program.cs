using TravelAgency.Media.API.Extensions;
using TravelAgency.Media.API.Middleware;
using TravelAgency.Media.Infrastructure;
using TravelAgency.Media.Infrastructure.Extensions;
using TravelAgency.Media.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.Hosting;
using TravelAgency.Shared.Infrastructure.Logging;
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
    /// gRPC is a second listener, HTTP/2 only (8081 by default).
    /// </summary>
    public static void ConfigureHost(IWebHostBuilder host) => host.UseServiceListenPorts();

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
        app.UseGrpcListenPortGuard();
        app.UseMediaCors();

        app.UseTravelAgencyRequestLogging();
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
        app.MapGrpcServiceOnGrpcPort<MediaGrpcService>();
        app.MapMediaHealthChecks();
    }
}
