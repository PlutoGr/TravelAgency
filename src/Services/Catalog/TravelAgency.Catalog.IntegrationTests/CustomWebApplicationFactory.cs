using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using TravelAgency.Catalog.Infrastructure.Persistence;

namespace TravelAgency.Catalog.IntegrationTests;

/// <summary>
/// Custom test factory that bypasses WebApplicationFactory's HostFactoryResolver
/// (which hangs with Minimal API + Serilog) by using TestServer directly.
/// </summary>
public class CustomWebApplicationFactory : IAsyncDisposable
{
    private readonly SqliteConnection _connection;
    private WebApplication? _app;
    private TestServer? _server;

    public CustomWebApplicationFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
    }

    public async Task InitializeAsync()
    {
        var testSettings = new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "TestIssuer",
            ["JwtSettings:Audience"] = "TestAudience",
            ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
            ["ConnectionStrings:CatalogDb"] = "DataSource=:memory:",
            ["Serilog:MinimumLevel:Default"] = "Warning"
        };

        var builder = WebApplication.CreateBuilder();

        builder.Configuration.AddInMemoryCollection(testSettings);

        builder.WebHost
            .UseTestServer()
            .UseEnvironment("Testing");

        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);

        // Register all application services
        Program.ConfigureServices(builder.Services, builder.Configuration);

        // Replace DbContext with SQLite in-memory
        builder.Services.RemoveAll<DbContextOptions<CatalogDbContext>>();
        builder.Services.RemoveAll<CatalogDbContext>();

        var sqliteOptions = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
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
