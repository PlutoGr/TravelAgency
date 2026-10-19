using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using TravelAgency.Media.Infrastructure.Persistence;

namespace TravelAgency.Media.IntegrationTests.Migrations;

public class ClearNonTourImagePublicFlagMigrationTests : IAsyncLifetime
{
    private const string PreviousMigration = "20261008050817_MarkMediaFileIdClientGenerated";
    private const string ThisMigration = "20261019074642_ClearNonTourImagePublicFlag";

    private static readonly Guid PublicGeneral = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid PrivateGeneral = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid PublicTour = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PrivateTour = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid PublicTourMixedCase = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid PublicEmptyPurpose = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("media_public_flag")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Up_RepeatUp_AndDown_ClearOnlyNonTourImages_AndRestoreSavedIds()
    {
        var logs = new CollectingLogger();
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .AddInterceptors(new PostgresNoticeLoggingInterceptor(logs))
            .Options;

        await using var db = new MediaDbContext(options);
        await db.Database.MigrateAsync(PreviousMigration);
        await InsertAsync(db, PublicGeneral, isPublic: true, purpose: "general");
        await InsertAsync(db, PrivateGeneral, isPublic: false, purpose: "general");
        await InsertAsync(db, PublicTour, isPublic: true, purpose: "tour-image");
        await InsertAsync(db, PrivateTour, isPublic: false, purpose: "tour-image");
        await InsertAsync(db, PublicTourMixedCase, isPublic: true, purpose: "TOUR-IMAGE");
        await InsertAsync(db, PublicEmptyPurpose, isPublic: true, purpose: "");

        await db.Database.MigrateAsync(ThisMigration);

        var afterUp = await ReadAsync(db);
        afterUp[PublicGeneral].IsPublic.Should().BeFalse();
        afterUp[PublicEmptyPurpose].IsPublic.Should().BeFalse();
        afterUp[PrivateGeneral].IsPublic.Should().BeFalse();
        afterUp[PublicTour].IsPublic.Should().BeTrue();
        afterUp[PrivateTour].IsPublic.Should().BeFalse();
        afterUp[PublicTourMixedCase].IsPublic.Should().BeTrue();
        (await LedgerAsync(db)).Should().BeEquivalentTo([PublicGeneral, PublicEmptyPurpose]);
        logs.Messages.Should().Contain("MediaFiles.IsPublic cleared: 2");

        await SetPublicAsync(db, PublicGeneral, true);
        await SetPublicAsync(db, PrivateGeneral, true);
        var beforeRepeat = await ReadAsync(db);

        await db.Database.MigrateAsync(ThisMigration);
        (await ReadAsync(db)).Should().BeEquivalentTo(beforeRepeat);

        await db.Database.ExecuteSqlInterpolatedAsync(
            $"DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = {ThisMigration}");
        var logged = logs.Messages.Count;
        await db.Database.MigrateAsync(ThisMigration);

        (await ReadAsync(db)).Should().BeEquivalentTo(beforeRepeat);
        (await LedgerAsync(db)).Should().BeEquivalentTo([PublicGeneral, PublicEmptyPurpose]);
        logs.Messages.Skip(logged).Should().Contain("MediaFiles.IsPublic cleared: 0");

        await SetPublicAsync(db, PublicGeneral, false);
        await SetPublicAsync(db, PrivateGeneral, false);
        var beforeDown = await ReadAsync(db);

        await db.Database.MigrateAsync(PreviousMigration);

        var afterDown = await ReadAsync(db);
        afterDown.Should().HaveCount(beforeDown.Count);
        afterDown[PublicGeneral].IsPublic.Should().BeTrue();
        afterDown[PublicEmptyPurpose].IsPublic.Should().BeTrue();
        afterDown[PrivateGeneral].IsPublic.Should().BeFalse();
        afterDown[PublicTour].IsPublic.Should().BeTrue();
        afterDown[PrivateTour].IsPublic.Should().BeFalse();
        afterDown[PublicTourMixedCase].IsPublic.Should().BeTrue();
        afterDown.Values.Select(row => (row.Id, row.Purpose, row.OriginalFileName))
            .Should().BeEquivalentTo(beforeDown.Values.Select(row => (row.Id, row.Purpose, row.OriginalFileName)));
        (await LedgerExistsAsync(db)).Should().BeFalse();
        logs.Messages.Should().Contain("MediaFiles.IsPublic restored: 2");
    }

    private static async Task InsertAsync(MediaDbContext db, Guid id, bool isPublic, string purpose)
    {
        var uploadedAt = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var key = "keys/" + id.ToString("N");
        var name = "file-" + id.ToString("N") + ".bin";
        await db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "MediaFiles"
                ("Id", "OriginalFileName", "ContentType", "SizeBytes", "StorageKey", "OwnerId", "Status", "UploadedAt", "IsPublic", "Purpose")
            VALUES
                ({id}, {name}, {"application/octet-stream"}, {12}, {key}, {"owner-1"}, {"Active"}, {uploadedAt}, {isPublic}, {purpose})
            """);
    }

    private static Task SetPublicAsync(MediaDbContext db, Guid id, bool isPublic) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "MediaFiles" SET "IsPublic" = {isPublic} WHERE "Id" = {id}""");

    private static async Task<Dictionary<Guid, StoredFile>> ReadAsync(MediaDbContext db)
    {
        var rows = new Dictionary<Guid, StoredFile>();
        var connection = db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened)
            await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT "Id", "IsPublic", "Purpose", "OriginalFileName"
                FROM "MediaFiles"
                """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var id = reader.GetGuid(0);
                rows[id] = new StoredFile(id, reader.GetBoolean(1), reader.GetString(2), reader.GetString(3));
            }
        }
        finally
        {
            if (opened)
                await connection.CloseAsync();
        }

        return rows;
    }

    private static async Task<List<Guid>> LedgerAsync(MediaDbContext db)
    {
        var ids = new List<Guid>();
        var connection = db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened)
            await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """SELECT "MediaFileId" FROM "MediaFilePublicFlagRollback" """;
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                ids.Add(reader.GetGuid(0));
        }
        finally
        {
            if (opened)
                await connection.CloseAsync();
        }

        return ids;
    }

    private static async Task<bool> LedgerExistsAsync(MediaDbContext db)
    {
        var connection = db.Database.GetDbConnection();
        var opened = connection.State != ConnectionState.Open;
        if (opened)
            await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """SELECT to_regclass('public."MediaFilePublicFlagRollback"') IS NOT NULL""";
            var result = await command.ExecuteScalarAsync();
            return result is bool exists && exists;
        }
        finally
        {
            if (opened)
                await connection.CloseAsync();
        }
    }

    private sealed record StoredFile(Guid Id, bool IsPublic, string Purpose, string OriginalFileName);

    private sealed class CollectingLogger : ILogger
    {
        public List<string> Messages { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
