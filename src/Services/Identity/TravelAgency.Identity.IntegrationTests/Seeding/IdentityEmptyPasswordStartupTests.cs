using System.Net;
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
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Identity.IntegrationTests.Seeding;

public class IdentityEmptyPasswordStartupTests
{
    [Fact]
    public async Task EmptyPassword_HostStarts_SkipsUsers_AndLogsOneWarningWithoutException()
    {
        await using var factory = new EmptyPasswordIdentityFactory();
        var client = factory.CreateClient();

        var live = await client.GetAsync("/health/live");
        live.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        (await db.Users.CountAsync()).Should().Be(0);

        var warnings = factory.Logs.Entries
            .Where(entry => entry.Category == typeof(IdentityDataSeeder).FullName && entry.Level == LogLevel.Warning)
            .ToList();
        warnings.Should().ContainSingle();
        warnings[0].Exception.Should().BeNull();
        warnings[0].Message.Should().Contain(DemoSeedGate.TestUserPasswordKey);
    }

    private sealed class EmptyPasswordIdentityFactory : WebApplicationFactory<Program>
    {
        private readonly SqliteConnection _connection = Open();

        public CollectingLoggerProvider Logs { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Staging");
            builder.UseSetting("ConnectionStrings:IdentityDb", "Server=localhost;Database=TestDb;");
            builder.UseSetting("JwtSettings:SigningKey", "TestSigningKeyWithAtLeast32CharactersForHMAC");
            builder.UseSetting("ASPNETCORE_RUN_MIGRATIONS", "false");
            builder.UseSetting("ASPNETCORE_SEED_DATA", "true");
            builder.UseSetting(DemoSeedGate.TestUserPasswordKey, "");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:Issuer"] = "TestIssuer",
                    ["JwtSettings:Audience"] = "TestAudience",
                    ["JwtSettings:SigningKey"] = "TestSigningKeyWithAtLeast32CharactersForHMAC",
                    ["JwtSettings:AccessTokenExpirationMinutes"] = "60",
                    ["JwtSettings:RefreshTokenExpirationDays"] = "7",
                    ["ConnectionStrings:IdentityDb"] = "Server=localhost;Database=TestDb;",
                    ["ConnectionStrings:Redis"] = string.Empty,
                    ["ASPNETCORE_RUN_MIGRATIONS"] = "false",
                    ["ASPNETCORE_SEED_DATA"] = "true",
                    [DemoSeedGate.TestUserPasswordKey] = ""
                });
            });

            builder.ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddProvider(Logs);
                logging.SetMinimumLevel(LogLevel.Warning);
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<IdentityDbContext>>();
                services.RemoveAll<IdentityDbContext>();
                var sqliteOptions = new DbContextOptionsBuilder<IdentityDbContext>()
                    .UseSqlite(_connection)
                    .Options;
                using (var ready = new IdentityDbContext(sqliteOptions))
                    ready.Database.EnsureCreated();

                services.AddScoped<IdentityDbContext>(_ => new IdentityDbContext(sqliteOptions));
                services.AddScoped<DbContextOptions<IdentityDbContext>>(_ => sqliteOptions);
                services.AddSingleton<ILogger<IdentityDataSeeder>>(new SeederLogger(Logs));
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
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _connection.DisposeAsync();
        }

        private static SqliteConnection Open()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            return connection;
        }
    }

    private sealed class SeederLogger(CollectingLoggerProvider logs) : ILogger<IdentityDataSeeder>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            logs.Entries.Add((typeof(IdentityDataSeeder).FullName!, logLevel, formatter(state, exception), exception));
        }
    }

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public List<(string Category, LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CollectingLogger(categoryName, Entries);

        public void Dispose()
        {
        }

        private sealed class CollectingLogger(
            string category,
            List<(string Category, LogLevel Level, string Message, Exception? Exception)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                entries.Add((category, logLevel, formatter(state, exception), exception));
            }
        }
    }
}
