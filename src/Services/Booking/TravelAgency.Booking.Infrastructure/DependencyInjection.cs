using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Booking.Application;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Contracts.Grpc.Catalog;
using TravelAgency.Contracts.Grpc.Identity;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Infrastructure.GrpcClients;
using TravelAgency.Booking.Infrastructure.Persistence;
using TravelAgency.Booking.Infrastructure.Repositories;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Booking.Infrastructure;

public static class DependencyInjection
{
    public static IApplicationBuilder UseBookingMigrations(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var runMigrations = string.Equals(
            configuration["ASPNETCORE_RUN_MIGRATIONS"],
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
        services.AddSharedMediatRBehaviors();

        var connectionString = configuration.GetConnectionString("BookingDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:BookingDb is required. Set ConnectionStrings__BookingDb environment variable or add it to configuration.");

        services.AddDbContext<BookingDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BookingDbContext>());
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IFavoriteRepository, FavoriteRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddHttpContextAccessor();
        services.AddCurrentUserService();

        services.AddGrpcAuthCallOptionsFactory();

        var catalogGrpcAddress = configuration["GrpcClients:CatalogServiceUrl"] ?? "http://catalog-service:8081";
        var identityGrpcAddress = configuration["GrpcClients:IdentityServiceUrl"] ?? "http://identity-service:8081";

        services.AddGrpcClient<CatalogService.CatalogServiceClient>(o => o.Address = new Uri(catalogGrpcAddress));
        services.AddGrpcClient<IdentityGrpc.IdentityGrpcClient>(o => o.Address = new Uri(identityGrpcAddress));

        services.AddScoped<ICatalogGrpcClient, CatalogGrpcClient>();
        services.AddScoped<IIdentityGrpcClient, IdentityGrpcClient>();

        return services;
    }
}
