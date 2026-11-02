using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Repositories;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.Infrastructure;

public class AccountLinkPostgresTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("identity_links")
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
    public async Task Tk21_ParallelConsumeChangesOnce_AndMigrationAddsOnlyTheTwoTables()
    {
        await using (var db = new IdentityDbContext(_options))
        {
            await db.Database.MigrateAsync();
        }

        var user = User.Create("parallel@example.com", new PasswordHasherService().Hash("old-password"), "Анна", "Смирнова", null);
        var secret = LinkSecret.Create();
        var reset = PasswordResetToken.Create(user.Id, secret, DateTime.UtcNow.AddHours(1));
        var confirm = LinkSecret.Create();
        var cancel = LinkSecret.Create();
        var change = EmailChangeRequest.Create(user.Id, "next@example.com", confirm, cancel, DateTime.UtcNow.AddHours(24));
        await using (var db = new IdentityDbContext(_options))
        {
            db.Users.Add(user);
            db.PasswordResetTokens.Add(reset);
            db.EmailChangeRequests.Add(change);
            await db.SaveChangesAsync();
        }

        var resetResults = await Task.WhenAll(
            ConsumeReset(reset.Id, "Password1"),
            ConsumeReset(reset.Id, "Password2"));
        resetResults.Count(result => result).Should().Be(1);

        var confirmResults = await Task.WhenAll(
            ConsumeConfirm(change.Id),
            ConsumeConfirm(change.Id));
        confirmResults.Count(result => result == Application.Abstractions.EmailChangeConsumption.Changed).Should().Be(1);

        await using (var db = new IdentityDbContext(_options))
        {
            var stored = await db.Users.AsNoTracking().SingleAsync(item => item.Id == user.Id);
            stored.Email.Should().Be("next@example.com");
            var fresh = EmailChangeRequest.Create(user.Id, "third@example.com", LinkSecret.Create(), LinkSecret.Create(), DateTime.UtcNow.AddHours(24));
            db.EmailChangeRequests.Add(fresh);
            await db.SaveChangesAsync();
            var cancelResults = await Task.WhenAll(ConsumeCancel(fresh.Id), ConsumeCancel(fresh.Id));
            cancelResults.Count(result => result).Should().Be(1);
        }

        await using (var db = new IdentityDbContext(_options))
        {
            var tables = await TableNames(db);
            tables.Should().Contain(new[] { "PasswordResetTokens", "EmailChangeRequests" });
            var userColumns = await ColumnNames(db, "Users");
            userColumns.Should().NotContain(new[] { "NewEmail", "PendingEmail" });
            await db.Database.MigrateAsync("20261102140000_DropNarrowedProfileColumns");
        }

        await using (var db = new IdentityDbContext(_options))
        {
            var tables = await TableNames(db);
            tables.Should().NotContain("PasswordResetTokens");
            tables.Should().NotContain("EmailChangeRequests");
        }
    }

    private async Task<bool> ConsumeReset(Guid id, string password)
    {
        await using var db = new IdentityDbContext(_options);
        var store = new AccountLinkStore(db);
        var user = await db.Users.SingleAsync(item => item.Email == "parallel@example.com");
        return await store.TryConsumePasswordResetAsync(id, user, new PasswordHasherService().Hash(password), CancellationToken.None);
    }

    private async Task<Application.Abstractions.EmailChangeConsumption> ConsumeConfirm(Guid id)
    {
        await using var db = new IdentityDbContext(_options);
        var request = await db.EmailChangeRequests.AsNoTracking().SingleAsync(item => item.Id == id);
        var user = await db.Users.SingleAsync(item => item.Id == request.UserId);
        return await new AccountLinkStore(db).TryConsumeEmailConfirmationAsync(
            id,
            user,
            request.NewEmail,
            keepRefreshId: null,
            CancellationToken.None);
    }

    private async Task<bool> ConsumeCancel(Guid id)
    {
        await using var db = new IdentityDbContext(_options);
        return await new AccountLinkStore(db).TryConsumeEmailCancellationAsync(id, CancellationToken.None);
    }

    private static async Task<List<string>> TableNames(IdentityDbContext db)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'", connection);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }

    private static async Task<List<string>> ColumnNames(IdentityDbContext db, string table)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
            await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT column_name FROM information_schema.columns WHERE table_name = @table", connection);
        command.Parameters.AddWithValue("table", table);
        var names = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            names.Add(reader.GetString(0));
        return names;
    }
}
