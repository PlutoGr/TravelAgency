using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Catalog.Application;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Interfaces;
using TravelAgency.Catalog.Infrastructure.GrpcClients;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.Infrastructure.Queries;
using TravelAgency.Catalog.Infrastructure.Repositories;
using TravelAgency.Catalog.Infrastructure.Seeding;
using TravelAgency.Contracts.Grpc.Media;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Catalog.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup when RunMigrations is enabled
    /// (e.g. in Docker or development) so the database is ready for real data.
    /// </summary>
    public static IApplicationBuilder UseCatalogMigrations(this IApplicationBuilder app)
    {
        // Флаг берётся из конфигурации хоста, куда уже попала переменная окружения.
        // Так тестовый хост может выставить false и не подхватить чужой процессный env.
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var runMigrations = string.Equals(
            configuration["ASPNETCORE_RUN_MIGRATIONS"],
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!runMigrations && !app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            return app;

        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Database.Migrate();
        return app;
    }

    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddSharedMediatRBehaviors();

        var connectionString = configuration.GetConnectionString("CatalogDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:CatalogDb is required. Set ConnectionStrings__CatalogDb environment variable or add it to configuration.");

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<ITourRepository, TourRepository>();
        services.AddScoped<ITourListQuery, TourListQuery>();
        services.AddScoped<IPublicTourCardQuery, PublicTourCardQuery>();
        services.AddScoped<IDirectionRepository, DirectionRepository>();
        services.AddHostedService<CatalogDataSeeder>();

        services.AddHttpContextAccessor();
        services.AddCurrentUserService();
        services.AddGrpcAuthCallOptionsFactory();

        var mediaGrpcAddress = configuration["GrpcClients:MediaServiceUrl"] ?? "http://media-service:8081";
        services.AddGrpcClient<MediaService.MediaServiceClient>(options => options.Address = new Uri(mediaGrpcAddress));
        services.AddScoped<IMediaFilesClient, MediaFilesGrpcClient>();

        return services;
    }
}
