using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TravelAgency.Identity.Application;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Repositories;
using TravelAgency.Identity.Infrastructure.Seeding;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Identity.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup when RunMigrations is enabled
    /// (e.g. in Docker or development) so the database is ready for real data.
    /// </summary>
    public static IApplicationBuilder UseIdentityMigrations(this IApplicationBuilder app)
    {
        var runMigrations = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_RUN_MIGRATIONS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!runMigrations && !app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            return app;

        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        db.Database.Migrate();
        return app;
    }

    public static IServiceCollection AddIdentityInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddSharedMediatRBehaviors();

        var connectionString = configuration.GetConnectionString("IdentityDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:IdentityDb is required. Set ConnectionStrings__IdentityDb environment variable or add it to configuration.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        services.Configure<LockoutSettings>(configuration.GetSection(LockoutSettings.SectionName));

        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Lockout:RedisConnection"];
        var useRedis = !string.IsNullOrWhiteSpace(redisConnection);

        if (useRedis)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection!));
            services.AddSingleton<ILockoutService, RedisLockoutService>();
        }
        else
        {
            services.AddSingleton<ILockoutService, InMemoryLockoutService>();
        }
        services.AddHttpContextAccessor();
        services.AddCurrentUserService();
        services.AddHostedService<IdentityDataSeeder>();

        return services;
    }
}
