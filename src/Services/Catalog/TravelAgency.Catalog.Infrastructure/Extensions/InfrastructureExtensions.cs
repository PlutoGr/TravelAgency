using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Catalog.Application;
using TravelAgency.Shared.Infrastructure.Extensions;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Domain.Interfaces;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.Infrastructure.Queries;
using TravelAgency.Catalog.Infrastructure.Repositories;

namespace TravelAgency.Catalog.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup when RunMigrations is enabled
    /// (e.g. in Docker or development) so the database is ready for real data.
    /// </summary>
    public static IApplicationBuilder UseCatalogMigrations(this IApplicationBuilder app)
    {
        var runMigrations = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_RUN_MIGRATIONS"),
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
        services.AddScoped<IDirectionRepository, DirectionRepository>();

        return services;
    }
}
