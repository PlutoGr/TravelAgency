using TravelAgency.Catalog.API.Extensions;
using TravelAgency.Catalog.API.Middleware;
using TravelAgency.Catalog.Infrastructure.Extensions;
using TravelAgency.Catalog.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.Hosting;
using TravelAgency.Shared.Infrastructure.Logging;
using TravelAgency.Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddCatalogSerilog();
builder.UseServiceListenPorts();
builder.Services.AddSingleton<IExceptionMapper, CatalogExceptionMapper>();

Program.ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

Program.ConfigurePipeline(app);

app.Run();

public partial class Program
{
    public static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCatalogInfrastructure(configuration);
        services.AddCatalogAuthentication(configuration);
        services.AddCatalogTracing();
        services.AddCatalogCors(configuration);
        services.AddCatalogHealthChecks(configuration);
        services.AddCatalogSwagger();
        services.AddControllers()
            .AddApplicationPart(typeof(TravelAgency.Catalog.API.Controllers.ToursController).Assembly);
        services.AddEndpointsApiExplorer();
        services.AddSingleton<GrpcAuthInterceptor>();
        services.AddGrpc(options => options.Interceptors.Add<GrpcAuthInterceptor>());
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseGrpcListenPortGuard();
        app.UseTravelAgencyRequestLogging();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
        app.UseCatalogCors();

        app.UseCatalogMigrations();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapGrpcServiceOnGrpcPort<CatalogGrpcService>();
        app.MapHealthChecks("/health/live");
        app.MapHealthChecks("/health/ready");
    }
}

