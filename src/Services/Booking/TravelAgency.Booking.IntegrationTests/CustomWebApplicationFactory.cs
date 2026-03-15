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
using TravelAgency.Booking.Infrastructure.Persistence;

namespace TravelAgency.Booking.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public Mock<ICatalogGrpcClient> CatalogGrpcClientMock { get; } = new();
    public Mock<IIdentityGrpcClient> IdentityGrpcClientMock { get; } = new();

    public CustomWebApplicationFactory()
    {
        // AddBookingInfrastructure and AddBookingAuthentication read config eagerly during host build.
        Environment.SetEnvironmentVariable("ConnectionStrings__BookingDb", "Host=localhost;Database=travel_booking_test");
        Environment.SetEnvironmentVariable("JwtSettings__SigningKey", "TestSigningKeyWithAtLeast32CharactersForHMAC");

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        // Default: return a sensible tour snapshot so CreateBooking doesn't need extra setup
        CatalogGrpcClientMock
            .Setup(c => c.GetTourSnapshotAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid tourId, CancellationToken _) => new BookingTourSnapshotDto(
                tourId, "Test Tour", "A great tour", 999.99m, "USD", 7, DateTime.UtcNow));

        // Default: return null for Identity (client details not enriched in tests unless explicitly set up)
        IdentityGrpcClientMock
            .Setup(c => c.GetUserSummaryAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSummary?)null);
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
            services.AddSingleton<ICatalogGrpcClient>(_ => CatalogGrpcClientMock.Object);
            services.RemoveAll<IIdentityGrpcClient>();
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
                ["JwtSettings:AccessTokenExpirationMinutes"] = "15",
                ["JwtSettings:ValidateLifetime"] = "true",
                ["ConnectionStrings:BookingDb"] = "Server=localhost;Database=TestDb;",
                ["GrpcClients:CatalogServiceUrl"] = "http://localhost:5000",
                ["GrpcSettings:InternalServiceToken"] = "test-internal-token",
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
