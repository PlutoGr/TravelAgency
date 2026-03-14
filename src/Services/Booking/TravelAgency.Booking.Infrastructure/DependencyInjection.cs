using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Booking.Application;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Infrastructure.BackgroundServices;
using TravelAgency.Booking.Infrastructure.GrpcClients;
using TravelAgency.Booking.Infrastructure.Persistence;
using TravelAgency.Booking.Infrastructure.Repositories;
using TravelAgency.Booking.Infrastructure.Services;

namespace TravelAgency.Booking.Infrastructure;

public static class DependencyInjection
{
    public static IApplicationBuilder UseBookingMigrations(this IApplicationBuilder app)
    {
        var runMigrations = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_RUN_MIGRATIONS"),
            "true", StringComparison.OrdinalIgnoreCase);

        if (!runMigrations && !app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return app;

        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        db.Database.Migrate();
        return app;
    }

    public static IServiceCollection AddBookingInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddBookingApplication();

        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("BookingDb")));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BookingDbContext>());
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddHttpContextAccessor();

        var catalogGrpcAddress = configuration["GrpcClients:CatalogServiceUrl"] ?? "http://catalog-service:8080";
        var identityGrpcAddress = configuration["GrpcClients:IdentityServiceUrl"] ?? "http://identity-service:8080";

        services.AddGrpcClient<CatalogGrpc.CatalogGrpcClient>(o => o.Address = new Uri(catalogGrpcAddress));
        services.AddGrpcClient<IdentityGrpc.IdentityGrpcClient>(o => o.Address = new Uri(identityGrpcAddress));

        services.AddScoped<ICatalogGrpcClient, CatalogGrpcClient>();
        services.AddScoped<IIdentityGrpcClient, IdentityGrpcClient>();

        services.AddHostedService<OutboxProcessorBackgroundService>();

        return services;
    }
}
