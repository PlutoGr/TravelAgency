using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Seeding;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Identity.IntegrationTests.Seeding;

public class IdentityDataSeederTests
{
    private const string FixtureSecret = "fixture-secret";

    [Fact]
    public async Task Development_AssignsStableIds_AndSecondRunDoesNotDuplicate()
    {
        await using var db = await CreateDbAsync();
        var hasher = new PasswordHasherService();

        await IdentityDataSeeder.SeedAsync(db, hasher, ConfigWithSecret(), NullLogger.Instance, CancellationToken.None);
        await IdentityDataSeeder.SeedAsync(db, hasher, ConfigWithSecret(), NullLogger.Instance, CancellationToken.None);

        var users = await db.Users.AsNoTracking().ToListAsync();
        users.Should().HaveCount(4);
        users.Should().ContainSingle(u => u.Id == DemoSeedIds.ClientId && u.Email == "client@test.com" && u.Role == UserRole.Client);
        users.Should().ContainSingle(u => u.Id == DemoSeedIds.ManagerId && u.Email == "manager@test.com" && u.Role == UserRole.Manager);
        users.Should().ContainSingle(u => u.Id == DemoSeedIds.AdminId && u.Email == "admin@test.com" && u.Role == UserRole.Admin);
        users.Should().ContainSingle(u => u.Id == DemoSeedIds.Manager2Id && u.Email == "manager2@test.com" && u.Role == UserRole.Manager);
        hasher.Verify(FixtureSecret, users.Single(u => u.Id == DemoSeedIds.Manager2Id).PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task ExistingUserWithAnotherId_IsKept_WithoutASecondRow()
    {
        await using var db = await CreateDbAsync();
        var hasher = new PasswordHasherService();
        var old = User.Create("manager@test.com", hasher.Hash(FixtureSecret), "Менеджер", "Тестов", null, UserRole.Manager);
        db.Users.Add(old);
        await db.SaveChangesAsync();
        var oldId = old.Id;

        await IdentityDataSeeder.SeedAsync(db, hasher, ConfigWithSecret(), NullLogger.Instance, CancellationToken.None);

        var managers = await db.Users.AsNoTracking().Where(u => u.Email == "manager@test.com").ToListAsync();
        managers.Should().ContainSingle();
        managers[0].Id.Should().Be(oldId);
        managers[0].Id.Should().NotBe(DemoSeedIds.ManagerId);
        (await db.Users.CountAsync(u => u.Email == "manager2@test.com")).Should().Be(1);
        (await db.Users.SingleAsync(u => u.Email == "manager2@test.com")).Id.Should().Be(DemoSeedIds.Manager2Id);
    }

    [Fact]
    public async Task WithoutConfiguredSecret_DoesNotCreateUsers_AndLogsOneWarning()
    {
        await using var db = await CreateDbAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DemoSeedGate.TestUserPasswordKey] = "  "
        }).Build();
        var logger = new CollectingLogger();

        await IdentityDataSeeder.SeedAsync(db, new PasswordHasherService(), config, logger, CancellationToken.None);

        (await db.Users.CountAsync()).Should().Be(0);
        var warnings = logger.Entries.Where(e => e.Level == Microsoft.Extensions.Logging.LogLevel.Warning).ToList();
        warnings.Should().ContainSingle();
        warnings[0].Exception.Should().BeNull();
        warnings[0].Message.Should().Contain(DemoSeedGate.TestUserPasswordKey);
    }

    [Fact]
    public async Task Production_DoesNotSeed_EvenWhenTheFlagIsSet()
    {
        await using var db = await CreateDbAsync();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DemoSeedGate.SeedDataKey] = "true",
            [DemoSeedGate.TestUserPasswordKey] = FixtureSecret
        }).Build();

        var seeder = new IdentityDataSeeder(
            Scope(db),
            new TestEnvironment(Environments.Production),
            config,
            NullLogger<IdentityDataSeeder>.Instance);

        await seeder.StartAsync(CancellationToken.None);

        (await db.Users.CountAsync()).Should().Be(0);
    }

    private static IConfiguration ConfigWithSecret() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DemoSeedGate.TestUserPasswordKey] = FixtureSecret
        }).Build();

    private static async Task<IdentityDbContext> CreateDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<IdentityDbContext>().UseSqlite(connection).Options;
        var db = new IdentityDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static IServiceScopeFactory Scope(IdentityDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<IPasswordHasher, PasswordHasherService>();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private sealed class CollectingLogger : ILogger
    {
        public List<(Microsoft.Extensions.Logging.LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception), exception));
        }
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
