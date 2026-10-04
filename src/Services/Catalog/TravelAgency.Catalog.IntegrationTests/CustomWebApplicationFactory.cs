using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Catalog.API.Middleware;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Infrastructure.Persistence;
using TravelAgency.Catalog.IntegrationTests.Helpers;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Catalog.IntegrationTests;

/// <summary>
/// Options for migration startup tests. Environment name and ASPNETCORE_RUN_MIGRATIONS
/// are applied to this host's configuration only.
/// </summary>
public class CatalogMigrationTestOptions
{
    public string? EnvironmentName { get; set; }
    public string? AspNetCoreRunMigrations { get; set; }
}

/// <summary>
/// Custom test factory that bypasses WebApplicationFactory's HostFactoryResolver
/// (which hangs with Minimal API + Serilog) by using TestServer directly.
/// </summary>
public class CustomWebApplicationFactory : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private WebApplication? _app;
    private TestServer? _server;

    public FakeMediaFilesClient Media { get; } = new();

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    /// <summary>
    /// Initializes the test server with default configuration.
    /// </summary>
    public Task InitializeAsync() => InitializeAsync(null, null);

    /// <summary>
    /// Initializes the test server, optionally customizing config before startup.
    /// Used by startup validation tests to verify SigningKey requirements.
    /// </summary>
    public Task InitializeAsync(Action<Dictionary<string, string?>>? configureConfig) =>
        InitializeAsync(configureConfig, null);

    /// <summary>
    /// Initializes the test server with full control over config and migration flag.
    /// Used by CatalogMigrationStartupTests to verify UseCatalogMigrations conditional logic.
    /// </summary>
    public async Task InitializeAsync(
        Action<Dictionary<string, string?>>? configureConfig,
        CatalogMigrationTestOptions? migrationOptions)
    {
        var testSettings = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["ConnectionStrings:CatalogDb"] = "DataSource=:memory:",
            ["GrpcSettings:InternalServiceToken"] = "test-internal-token",
            ["Serilog:MinimumLevel:Default"] = "Warning",
            ["ASPNETCORE_RUN_MIGRATIONS"] = "false",
            ["ASPNETCORE_SEED_DATA"] = "false",
            ["JWT_SIGNING_KEY"] = ""
        };

        configureConfig?.Invoke(testSettings);

        if (migrationOptions?.AspNetCoreRunMigrations is { } value)
            testSettings["ASPNETCORE_RUN_MIGRATIONS"] = value;

        var environmentName = migrationOptions?.EnvironmentName ?? "Testing";

        // Set environment in options; UseEnvironment("Testing") after CreateBuilder causes
        // "The environment changed from "" to "Testing"" error.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });

        builder.Configuration.AddInMemoryCollection(testSettings);

        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton<IExceptionMapper, CatalogExceptionMapper>();

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Register all application services
        Program.ConfigureServices(builder.Services, builder.Configuration);

        builder.Services.RemoveAll<IMediaFilesClient>();
        builder.Services.AddSingleton<IMediaFilesClient>(Media);

        // Replace DbContext with SQLite in-memory
        builder.Services.RemoveAll<DbContextOptions<CatalogDbContext>>();
        builder.Services.RemoveAll<CatalogDbContext>();

        var sqliteOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;

        builder.Services.AddScoped<CatalogDbContext>(_ => new CatalogDbContext(sqliteOptions));
        builder.Services.AddScoped<DbContextOptions<CatalogDbContext>>(_ => sqliteOptions);

        // Override JWT validation to use test keys
        builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
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
                RoleClaimType = ClaimTypes.Role
            };
        });

        _app = builder.Build();

        Program.ConfigurePipeline(_app);

        await _app.StartAsync();

        _server = (TestServer)_app.Services.GetRequiredService<IServer>();
    }

    public HttpClient CreateClient()
    {
        if (_server == null)
            throw new InvalidOperationException("Factory not initialized. Call InitializeAsync() first.");
        return _server.CreateClient();
    }

    public IServiceProvider Services
    {
        get
        {
            if (_app == null)
                throw new InvalidOperationException("Factory not initialized.");
            return _app.Services;
        }
    }

    public void EnsureDbCreated()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        db.Database.EnsureCreated();
    }

    public void UseDbContext(Action<CatalogDbContext> action)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        action(db);
    }

    public string GenerateToken(string userId, string role)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("TestSigningKeyWithAtLeast32CharactersForHMAC"));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Role, role),
            new Claim(JwtRegisteredClaimNames.Sub, userId)
        };
        var token = new JwtSecurityToken(
            issuer: "TestIssuer",
            audience: "TestAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async ValueTask DisposeAsync()
    {
        _server?.Dispose();
        if (_app != null)
            await _app.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
