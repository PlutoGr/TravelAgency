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
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.IntegrationTests.API;
using TravelAgency.Identity.IntegrationTests.Fakes;

namespace TravelAgency.Identity.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    // Общая in-memory база живёт, пока открыто хотя бы одно соединение.
    // У каждого DbContext своё соединение: одно общее соединение на параллельные
    // запросы даёт SQLITE_BUSY «unable to delete/modify user-function due to active statements»
    // и ответ «A database error occurred.» вместо бизнес-ошибки.
    private readonly string _connectionString;
    private readonly SqliteConnection _keepAlive;

    /// <summary>
    /// Лимит политики auth для этого хоста. Не читается из процесса:
    /// иначе параллельный тест с жёстким лимитом даёт 429 чужим запросам.
    /// </summary>
    public int RateLimitPermit { get; set; } = 1000;

    public FakeAvatarMediaClient AvatarMedia { get; } = new();

    public CustomWebApplicationFactory()
    {
        _connectionString = $"Data Source=identity-{Guid.NewGuid():N};Mode=Memory;Cache=Shared";
        _keepAlive = new SqliteConnection(_connectionString);
        _keepAlive.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:IdentityDb", "Server=localhost;Database=TestDb;");
        builder.UseSetting("JwtSettings:SigningKey", "TestSigningKeyWithAtLeast32CharactersForHMAC");
        builder.UseSetting("ASPNETCORE_RUN_MIGRATIONS", "false");
        builder.UseSetting("ASPNETCORE_SEED_DATA", "false");
        builder.UseSetting("RateLimit:PermitLimit", RateLimitPermit.ToString());

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
            // Строка подключения, а не один SqliteConnection: параллельные запросы
            // получают разные соединения к общей базе (её держит _keepAlive).
            var sqliteOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                .UseSqlite(_connectionString)
                .Options;

            services.AddScoped<IdentityDbContext>(_ => new IdentityDbContext(sqliteOptions));
            services.AddScoped<DbContextOptions<IdentityDbContext>>(_ => sqliteOptions);

            services.RemoveAll<IAvatarMediaClient>();
            services.AddSingleton<IAvatarMediaClient>(AvatarMedia);

            services.RemoveAll<IUserAdminLock>();
            services.AddScoped<UserAdminLock>();
            services.AddScoped<IUserAdminLock>(sp => new DemoteRaceLock(
                sp.GetRequiredService<UserAdminLock>(),
                sp.GetRequiredService<IHttpContextAccessor>()));

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
                ["ConnectionStrings:IdentityDb"] = "Server=localhost;Database=TestDb;",
                ["ConnectionStrings:Redis"] = string.Empty,
                ["LockoutSettings:LockoutThreshold"] = "3",
                ["RateLimit:PermitLimit"] = RateLimitPermit.ToString(),
                ["ASPNETCORE_RUN_MIGRATIONS"] = "false",
                ["ASPNETCORE_SEED_DATA"] = "false"
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
        await _keepAlive.DisposeAsync();
    }
}
