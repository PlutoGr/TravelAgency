using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Infrastructure.BackgroundServices;
using TravelAgency.Booking.Infrastructure.Persistence;

namespace TravelAgency.Booking.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Keep the connection open for the factory lifetime so the SQLite in-memory
    // database persists across all requests made during a test class.
    private readonly SqliteConnection _connection;

    public Mock<ICatalogGrpcClient> CatalogGrpcClientMock { get; } = new();
    public Mock<IIdentityGrpcClient> IdentityGrpcClientMock { get; } = new();

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Default: return a sensible tour snapshot so CreateBooking doesn't need extra setup
        CatalogGrpcClientMock
            .Setup(c => c.GetTourSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tourId, CancellationToken _) => new TourSnapshotDto(
                tourId, "Test Tour", "A great tour", 999.99m, "USD", 7, DateTime.UtcNow));
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

        builder.ConfigureServices(services =>
        {
            // Remove the outbox background service so it doesn't run during tests
            var outboxDescriptor = services.FirstOrDefault(
                d => d.ImplementationType == typeof(OutboxProcessorBackgroundService));
            if (outboxDescriptor != null)
                services.Remove(outboxDescriptor);

            // Replace BookingDbContext with SQLite in-memory.
            // Build options directly via DbContextOptionsBuilder instead of AddDbContext.
            // AddDbContext registers provider infrastructure into the main DI container,
            // which causes a "two database providers registered" conflict with Npgsql.
            services.RemoveAll<DbContextOptions<BookingDbContext>>();
            services.RemoveAll<BookingDbContext>();

            var sqliteOptions = new DbContextOptionsBuilder<BookingDbContext>()
                .UseSqlite(_connection)
                .Options;

            services.AddScoped<BookingDbContext>(_ => new BookingDbContext(sqliteOptions));
            services.AddScoped<DbContextOptions<BookingDbContext>>(_ => sqliteOptions);

            // Re-register IUnitOfWork to resolve from the replaced BookingDbContext
            services.RemoveAll<IUnitOfWork>();
            services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<BookingDbContext>());

            // Replace gRPC client adapters with mocks so no real gRPC calls are made
            services.RemoveAll<ICatalogGrpcClient>();
            services.RemoveAll<IIdentityGrpcClient>();
            services.AddSingleton<ICatalogGrpcClient>(_ => CatalogGrpcClientMock.Object);
            services.AddSingleton<IIdentityGrpcClient>(_ => IdentityGrpcClientMock.Object);

            // Override JWT validation parameters after all service configuration runs.
            // AddBookingAuthentication reads config values eagerly, so PostConfigure is
            // the reliable way to ensure test tokens are accepted.
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "TestIssuer",
                    ValidAudience = "TestAudience",
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("TestSigningKeyWithAtLeast32CharactersForHMAC")),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = System.Security.Claims.ClaimTypes.Role
                };
            });
        });

        builder.ConfigureAppConfiguration((_, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:ValidateLifetime"] = "true",
                ["ConnectionStrings:BookingDb"] = "Server=localhost;Database=TestDb;",
                ["GrpcClients:CatalogServiceUrl"] = "http://localhost:5000",
                ["GrpcClients:IdentityServiceUrl"] = "http://localhost:5001",
            };
            config.AddInMemoryCollection(testSettings);
        });
    }

    public void UseDbContext(Action<BookingDbContext> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        action(db);
    }

    /// <summary>
    /// Creates the SQLite schema. Safe to call multiple times — EnsureCreated is idempotent.
    /// </summary>
    public void EnsureDbCreated()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        db.Database.EnsureCreated();
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
