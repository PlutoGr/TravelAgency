using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using Testcontainers.PostgreSql;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Seeding;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Identity.IntegrationTests.Migrations;

public class DropNarrowedProfileColumnsMigrationTests : IAsyncLifetime
{
    private const string PreviousMigration = "20261027060000_AddUserProfileAndAvatar";
    private static readonly string[] DroppedColumns = ["City", "About", "TourismSince", "LanguagesJson", "Specialization"];

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("identity_profile")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private DbContextOptions<IdentityDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Up_DropsNarrativeColumns_AndDown_RestoresEmptyOnes()
    {
        var userId = Guid.Parse("3f1c2a0e-7b4d-4e1a-9c2f-6a8b0d1e2f30");
        var avatarId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await using (var db = new IdentityDbContext(_options))
        {
            NpgsqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync(PreviousMigration);
            foreach (var column in DroppedColumns)
                db.Model.FindEntityType(typeof(User))!.FindProperty(column).Should().BeNull();

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "Users"
                    ("Id", "Email", "PasswordHash", "FirstName", "LastName", "Phone", "Role", "CreatedAt",
                     "City", "About", "TourismSince", "LanguagesJson", "Specialization", "AvatarMediaFileId")
                VALUES
                    ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13})
                """,
                userId,
                "marina@example.com",
                "hash",
                "Марина",
                "Иванова",
                "+79991234567",
                "Manager",
                new DateTime(2026, 10, 27, 0, 0, 0, DateTimeKind.Utc),
                "Сочи",
                "О себе",
                2018,
                "[\"ru\"]",
                "Море",
                avatarId);

            await db.Database.MigrateAsync();
        }

        await using (var db = new IdentityDbContext(_options))
        {
            var columns = await ColumnNamesAsync(db);
            foreach (var column in DroppedColumns)
                columns.Should().NotContain(column);
            columns.Should().Contain("AvatarMediaFileId");
            columns.Should().NotContain("Passport");

            var avatar = await ScalarAsync<Guid>(
                db,
                """SELECT "AvatarMediaFileId" FROM "Users" WHERE "Id" = @id""",
                userId);
            avatar.Should().Be(avatarId);

            await IdentityDataSeeder.SeedAsync(
                db,
                new PasswordHasherService(),
                new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [DemoSeedGate.TestUserPasswordKey] = "fixture-secret"
                }).Build(),
                NullLogger.Instance,
                CancellationToken.None);
            (await db.Users.CountAsync(user => user.Email == "client@test.com")).Should().Be(1);

            await db.Database.MigrateAsync(PreviousMigration);
        }

        await using var down = new IdentityDbContext(_options);
        var restored = await ColumnNamesAsync(down);
        foreach (var column in DroppedColumns)
            restored.Should().Contain(column);
        restored.Should().Contain("AvatarMediaFileId");
        var row = await ReadNarrativeAsync(down, userId);
        row.City.Should().BeNull();
        row.About.Should().BeNull();
        row.TourismSince.Should().BeNull();
        row.LanguagesJson.Should().BeNull();
        row.Specialization.Should().BeNull();
        row.AvatarMediaFileId.Should().Be(avatarId);
    }

    private static async Task<List<string>> ColumnNamesAsync(IdentityDbContext db)
    {
        return await ReadStringsAsync(
            db,
            """
            SELECT column_name
            FROM information_schema.columns
            WHERE table_name = 'Users'
            """);
    }

    private static async Task<NarrativeRow> ReadNarrativeAsync(IdentityDbContext db, Guid userId)
    {
        var connection = await OpenAsync(db);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "City", "About", "TourismSince", "LanguagesJson", "Specialization", "AvatarMediaFileId"
            FROM "Users"
            WHERE "Id" = @id
            """;
        var id = command.CreateParameter();
        id.ParameterName = "id";
        id.Value = userId;
        command.Parameters.Add(id);
        await using var reader = await command.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        return new NarrativeRow(
            reader.IsDBNull(0) ? null : reader.GetString(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetInt32(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.GetGuid(5));
    }

    private static async Task<T> ScalarAsync<T>(IdentityDbContext db, string sql, Guid userId)
    {
        var connection = await OpenAsync(db);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var id = command.CreateParameter();
        id.ParameterName = "id";
        id.Value = userId;
        command.Parameters.Add(id);
        var value = await command.ExecuteScalarAsync();
        return (T)value!;
    }

    private static async Task<List<string>> ReadStringsAsync(IdentityDbContext db, string sql)
    {
        var connection = await OpenAsync(db);
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }

    private static async Task<System.Data.Common.DbConnection> OpenAsync(IdentityDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync();
        return connection;
    }

    private sealed record NarrativeRow(
        string? City,
        string? About,
        int? TourismSince,
        string? LanguagesJson,
        string? Specialization,
        Guid AvatarMediaFileId);
}
