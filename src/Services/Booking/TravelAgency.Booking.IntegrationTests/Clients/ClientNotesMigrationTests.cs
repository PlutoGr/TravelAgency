using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Infrastructure.Persistence;

namespace TravelAgency.Booking.IntegrationTests.Clients;

[Collection(Bookings.BookingPostgresCollection.Name)]
public class ClientNotesMigrationTests
{
    private readonly Bookings.ProposalUtcWebApplicationFactory _factory;

    public ClientNotesMigrationTests(Bookings.ProposalUtcWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddClientNotes_CreatesTheTable_AndDownDropsIt()
    {
        var database = "travel_booking_notes_" + Guid.NewGuid().ToString("N");
        var adminCs = _factory.PostgresConnectionString;
        var notesCs = new NpgsqlConnectionStringBuilder(adminCs) { Database = database }.ConnectionString;

        await using (var admin = new NpgsqlConnection(adminCs))
        {
            await admin.OpenAsync();
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", admin);
            await create.ExecuteNonQueryAsync();
        }

        try
        {
            var options = new DbContextOptionsBuilder<BookingDbContext>().UseNpgsql(notesCs).Options;
            await using (var db = new BookingDbContext(options))
            {
                await db.Database.MigrateAsync("20261019135257_AddBookingAssignedManager");
                var migrations = db.GetService<IMigrationsAssembly>();
                var differ = db.GetService<IMigrationsModelDiffer>();
                var last = migrations.Migrations.Last();
                last.Key.Should().EndWith("_AddClientNotes");
                var migration = (Migration)Activator.CreateInstance(last.Value.AsType())!;
                differ.GetDifferences(
                        migration.TargetModel.GetRelationalModel(),
                        migrations.ModelSnapshot!.Model.GetRelationalModel())
                    .Should().BeEmpty();
            }

            await using (var db = new BookingDbContext(options))
            {
                await db.Database.MigrateAsync();
                (await db.ClientNotes.CountAsync()).Should().Be(0);
                db.ClientNotes.Add(ClientNote.Create(Guid.NewGuid(), Guid.NewGuid(), "просит вылет после 15 мая"));
                await db.SaveChangesAsync();
                (await db.ClientNotes.CountAsync()).Should().Be(1);
            }

            await using (var connection = new NpgsqlConnection(notesCs))
            {
                await connection.OpenAsync();
                var columns = new List<(string Name, string Type, string Nullable, int? Length)>();
                await using (var command = new NpgsqlCommand(
                    """
                    SELECT column_name, udt_name, is_nullable, character_maximum_length
                    FROM information_schema.columns
                    WHERE table_name = 'ClientNotes'
                    ORDER BY ordinal_position
                    """,
                    connection))
                await using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                        columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetInt32(3)));
                }

                columns.Should().Equal(
                    ("Id", "uuid", "NO", null),
                    ("ClientId", "uuid", "NO", null),
                    ("AuthorId", "uuid", "NO", null),
                    ("Text", "varchar", "NO", 500),
                    ("CreatedAt", "timestamptz", "NO", null));

                await using var index = new NpgsqlCommand(
                    "SELECT indexname FROM pg_indexes WHERE tablename = 'ClientNotes'",
                    connection);
                var names = new List<string>();
                await using var indexReader = await index.ExecuteReaderAsync();
                while (await indexReader.ReadAsync())
                    names.Add(indexReader.GetString(0));
                names.Should().Contain("IX_ClientNotes_ClientId");
            }

            await using (var connection = new NpgsqlConnection(notesCs))
            {
                await connection.OpenAsync();
                await using var foreignKeys = new NpgsqlCommand(
                    """
                    SELECT COUNT(*)
                    FROM pg_constraint
                    WHERE conrelid = '"ClientNotes"'::regclass AND contype = 'f'
                    """,
                    connection);
                Convert.ToInt32(await foreignKeys.ExecuteScalarAsync()).Should().Be(0);
            }

            await using (var db = new BookingDbContext(options))
            {
                await db.Database.MigrateAsync("20261019135257_AddBookingAssignedManager");
            }

            await using (var connection = new NpgsqlConnection(notesCs))
            {
                await connection.OpenAsync();
                await using var exists = new NpgsqlCommand("SELECT to_regclass('\"ClientNotes\"')", connection);
                (await exists.ExecuteScalarAsync()).Should().BeNull();
            }

            var root = RepoRoot();
            foreach (var relative in new[]
            {
                "src/Services/Identity",
                "src/Services/Chat",
                "src/Services/Catalog",
                "src/Services/Media",
                "src/Gateway"
            })
            {
                var hits = Directory.GetFiles(Path.Combine(root, relative), "*.cs", SearchOption.AllDirectories)
                    .Where(file => File.ReadAllText(file).Contains("ClientNotes", StringComparison.Ordinal));
                hits.Should().BeEmpty();
            }
        }
        finally
        {
            await using var admin = new NpgsqlConnection(adminCs);
            await admin.OpenAsync();
            await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{database}\" WITH (FORCE)", admin);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "TravelAgency.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new InvalidOperationException("Корень репозитория не найден.");
    }
}
