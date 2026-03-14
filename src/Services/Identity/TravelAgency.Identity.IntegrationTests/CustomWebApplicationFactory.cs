using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Keep the connection open for the factory lifetime so the SQLite in-memory
    // database persists across all requests made during a test class.
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
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
            services.RemoveAll<DbContextOptions<IdentityDbContext>>();
            services.RemoveAll<IdentityDbContext>();

            // Build options directly via DbContextOptionsBuilder instead of AddDbContext.
            // AddDbContext registers provider infrastructure into the main DI container,
            // which causes a "two database providers registered" conflict with Npgsql.
            // Using a factory bypasses that registration path entirely.
            //
            // Passing the open SqliteConnection (rather than a connection string) prevents
            // EF Core from closing it between requests — the in-memory database lives as
            // long as at least one connection is open.
            var sqliteOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(_connection)
                .Options;

            services.AddScoped<IdentityDbContext>(_ => new IdentityDbContext(sqliteOptions));
            services.AddScoped<DbContextOptions<IdentityDbContext>>(_ => sqliteOptions);

            // Override JWT validation parameters after all service configuration runs.
            // AddIdentityAuthentication reads config values eagerly, so PostConfigure is
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

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var testSettings = new Dictionary<string, string?>
            {
                ["JwtSettings:Issuer"] = "TestIssuer",
                ["JwtSettings:Audience"] = "TestAudience",
                ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
                ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                ["JwtSettings:ValidateLifetime"] = "true",
                ["ConnectionStrings:IdentityDb"] = "Server=localhost;Database=TestDb;"
            };

            config.AddInMemoryCollection(testSettings);
        });
    }

    public void UseDbContext(Action<IdentityDbContext> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        action(db);
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
