using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Catalog.Application;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Infrastructure.GrpcServices;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.Infrastructure.Repositories;

namespace TravelAgency.Catalog.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static IApplicationBuilder UseCatalogMigrations(this IApplicationBuilder app)
    {
        var runMigrations = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_RUN_MIGRATIONS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!runMigrations)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            if (!env.IsDevelopment()) return app;
        }

        using var migrateScope = app.ApplicationServices.CreateScope();
        var db = migrateScope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Database.Migrate();
        return app;
    }

    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();

        services.AddDbContext<CatalogDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("CatalogDb")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<ITourRepository, TourRepository>();
        services.AddScoped<IDirectionRepository, DirectionRepository>();
        services.AddScoped<CatalogGrpcService>();

        return services;
    }
}
