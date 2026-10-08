using Serilog;
using TravelAgency.Media.API.Extensions;
using TravelAgency.Media.API.Middleware;
using TravelAgency.Media.Infrastructure;
using TravelAgency.Media.Infrastructure.Extensions;
using TravelAgency.Media.Infrastructure.GrpcServices;
using TravelAgency.Media.Infrastructure.Maintenance;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.Hosting;
using TravelAgency.Shared.Infrastructure.Middleware;

var backfillRequested = MediaDimensionBackfillCommand.IsRequested(args);
var builder = WebApplication.CreateBuilder(MediaDimensionBackfillCommand.WithoutCommandArgs(args));

builder.Host.AddMediaSerilog();
Program.ConfigureHost(builder.WebHost);
Program.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

if (backfillRequested)
{
    try
    {
        await Program.ExecuteDimensionBackfillAsync(
            app.Services,
            MediaDimensionBackfillCommand.IsDryRun(args));
    }
    finally
    {
        await app.DisposeAsync();
    }

    return;
}

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
        app.MapGrpcServiceOnGrpcPort<MediaGrpcService>();
        app.MapMediaHealthChecks();
    }

    /// <summary>
    /// Runs only when the process was started with <c>backfill-dimensions</c>.
    /// Does not start Kestrel, hosted services, or migrations.
    /// </summary>
    public static async Task<MediaDimensionBackfillReport> ExecuteDimensionBackfillAsync(
        IServiceProvider services,
        bool dryRun,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var backfill = scope.ServiceProvider.GetRequiredService<MediaDimensionBackfill>();
        return await backfill.RunAsync(dryRun, cancellationToken);
    }
}
