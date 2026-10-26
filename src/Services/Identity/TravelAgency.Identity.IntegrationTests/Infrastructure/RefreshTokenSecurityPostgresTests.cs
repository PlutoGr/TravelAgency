using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;
using Testcontainers.PostgreSql;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Auth.Commands.RefreshToken;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Repositories;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.IntegrationTests.Infrastructure;

public class RefreshTokenSecurityPostgresTests : IAsyncLifetime
{
    private const string PreviousMigration = "20261008050812_MarkUserIdClientGenerated";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("identity_refresh")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly JwtSettings _jwtSettings = new()
    {
        Issuer = "TestIssuer",
        Audience = "TestAudience",
        SigningKey = "TestSigningKeyWithAtLeast32CharactersForHMAC",
        AccessTokenExpirationMinutes = 60,
        RefreshTokenExpirationDays = 7
    };

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
    public async Task Migration_HashesExistingTokensInPlace_AndRefreshStillWorks()
    {
        const string activePlain = "legacy+token/with=padding";
        const string revokedPlain = "legacy-revoked+token/=";
        const string replacedPlain = "legacy-replacement+token/=";

        await using (var db = new IdentityDbContext(_options))
        {
            NpgsqlConnection.ClearAllPools();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync(PreviousMigration);

            var userId = Guid.NewGuid();
            var activeId = Guid.NewGuid();
            var revokedId = Guid.NewGuid();
            var created = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);
            var expires = DateTime.UtcNow.AddDays(7);
            var revokedAt = created.AddHours(1);

            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FirstName", "LastName", "Phone", "Role", "CreatedAt", "UpdatedAt")
                VALUES ({0}, {1}, {2}, {3}, {4}, NULL, {5}, {6}, NULL)
                """,
                userId, "legacy@example.com", "hash", "J", "D", "Client", created);
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "RefreshTokens" ("Id", "UserId", "Token", "ExpiresAt", "CreatedAt", "RevokedAt", "ReplacedByToken")
                VALUES ({0}, {1}, {2}, {3}, {4}, NULL, NULL)
                """,
                activeId, userId, activePlain, expires, created);
            await db.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO "RefreshTokens" ("Id", "UserId", "Token", "ExpiresAt", "CreatedAt", "RevokedAt", "ReplacedByToken")
                VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})
                """,
                revokedId, userId, revokedPlain, expires, created, revokedAt, replacedPlain);

            await db.Database.MigrateAsync();
        }

        await using var read = new IdentityDbContext(_options);
        var repository = new RefreshTokenRepository(read);
        var active = await repository.GetByTokenAsync(activePlain);
        active.Should().NotBeNull();
        active!.Token.Should().Be(RefreshToken.Hash(activePlain));
        active.Token.Should().NotBe(activePlain);
        active.IsRevoked.Should().BeFalse();
        active.ReplacedByToken.Should().BeNull();

        var revoked = await repository.GetByTokenAsync(revokedPlain);
        revoked.Should().NotBeNull();
        revoked!.Token.Should().Be(RefreshToken.Hash(revokedPlain));
        revoked.ReplacedByToken.Should().Be(RefreshToken.Hash(replacedPlain));
        revoked.ReplacedByToken.Should().NotBe(replacedPlain);

        var handler = CreateHandler(read, new RefreshTokenRepository(read));
        var refreshed = await handler.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest(activePlain)),
            CancellationToken.None);

        refreshed.RefreshToken.Should().NotBeNullOrWhiteSpace();
        var rows = await read.RefreshTokens.AsNoTracking().ToListAsync();
        rows.Should().OnlyContain(row => IsSha256Hex(row.Token) && (row.ReplacedByToken == null || IsSha256Hex(row.ReplacedByToken)));
        rows.Select(row => row.Token).Should().NotContain(activePlain).And.NotContain(revokedPlain);
        rows.Select(row => row.ReplacedByToken).Should().NotContain(replacedPlain);
        rows.Select(row => row.Token).Should().NotContain(refreshed.RefreshToken);
    }

    [Fact]
    public async Task TwoParallelRefreshes_ExactlyOneSucceeds()
    {
        await ResetAndMigrateAsync();
        const string plain = "shared-refresh-token";
        await using (var seed = new IdentityDbContext(_options))
        {
            var user = User.Create("race@example.com", "hash", "J", "D", null);
            seed.Users.Add(user);
            seed.RefreshTokens.Add(RefreshToken.Create(user.Id, plain, DateTime.UtcNow.AddDays(7)));
            await seed.SaveChangesAsync();
        }

        var reads = 0;
        var bothRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<(AuthTokensDto? Tokens, Exception? Error)> RefreshOnce()
        {
            await using var db = new IdentityDbContext(_options);
            var repository = new ReadGateRepository(new RefreshTokenRepository(db), async () =>
            {
                if (Interlocked.Increment(ref reads) == 2)
                    bothRead.TrySetResult();
                await bothRead.Task.WaitAsync(TimeSpan.FromSeconds(20));
            });
            var handler = CreateHandler(db, repository);
            try
            {
                var tokens = await handler.Handle(
                    new RefreshTokenCommand(new RefreshTokenRequest(plain)),
                    CancellationToken.None);
                return (tokens, null);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }

        var results = await Task.WhenAll(RefreshOnce(), RefreshOnce());
        var succeeded = results.Where(result => result.Tokens is not null).ToArray();
        var failed = results.Where(result => result.Error is not null).ToArray();

        succeeded.Should().ContainSingle();
        failed.Should().ContainSingle();
        failed[0].Error.Should().BeOfType<UnauthorizedException>();

        var winner = succeeded[0].Tokens!;
        await using var check = new IdentityDbContext(_options);
        var rows = await check.RefreshTokens.AsNoTracking().ToListAsync();
        rows.Should().OnlyContain(row => IsSha256Hex(row.Token) && (row.ReplacedByToken == null || IsSha256Hex(row.ReplacedByToken)));
        rows.Should().ContainSingle(row => row.RevokedAt == null)
            .Which.Token.Should().Be(RefreshToken.Hash(winner.RefreshToken));
        rows.Should().ContainSingle(row => row.Token == RefreshToken.Hash(plain) && row.RevokedAt != null);

        var followUp = CreateHandler(check, new RefreshTokenRepository(check));
        var next = await followUp.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest(winner.RefreshToken)),
            CancellationToken.None);
        next.RefreshToken.Should().NotBe(winner.RefreshToken);
    }

    [Fact]
    public async Task ReuseOfRevokedToken_RevokesAllActiveTokensForThatUser()
    {
        await ResetAndMigrateAsync();
        const string stolen = "stolen-refresh";
        const string otherSession = "other-session";
        Guid userId;
        await using (var seed = new IdentityDbContext(_options))
        {
            var user = User.Create("reuse@example.com", "hash", "J", "D", null);
            var other = User.Create("other@example.com", "hash", "O", "T", null);
            userId = user.Id;
            seed.Users.AddRange(user, other);
            seed.RefreshTokens.Add(RefreshToken.Create(user.Id, stolen, DateTime.UtcNow.AddDays(7)));
            seed.RefreshTokens.Add(RefreshToken.Create(user.Id, otherSession, DateTime.UtcNow.AddDays(7)));
            seed.RefreshTokens.Add(RefreshToken.Create(other.Id, "untouched-session", DateTime.UtcNow.AddDays(7)));
            await seed.SaveChangesAsync();
        }

        await using (var first = new IdentityDbContext(_options))
        {
            var handler = CreateHandler(first, new RefreshTokenRepository(first));
            var rotated = await handler.Handle(
                new RefreshTokenCommand(new RefreshTokenRequest(stolen)),
                CancellationToken.None);
            rotated.RefreshToken.Should().NotBe(stolen);
        }

        var logger = new CollectingLogger();
        await using (var second = new IdentityDbContext(_options))
        {
            var handler = CreateHandler(second, new RefreshTokenRepository(second), logger);
            var act = async () => await handler.Handle(
                new RefreshTokenCommand(new RefreshTokenRequest(stolen)),
                CancellationToken.None);
            await act.Should().ThrowAsync<UnauthorizedException>();
        }

        logger.Messages.Should().ContainSingle();
        logger.Messages[0].Should().NotContain(stolen);
        logger.Messages[0].Should().Contain(userId.ToString());

        await using var check = new IdentityDbContext(_options);
        var userRows = await check.RefreshTokens.AsNoTracking().Where(token => token.UserId == userId).ToListAsync();
        userRows.Should().HaveCount(3);
        userRows.Should().OnlyContain(row => row.RevokedAt != null && IsSha256Hex(row.Token));
        var otherRows = await check.RefreshTokens.AsNoTracking().Where(token => token.UserId != userId).ToListAsync();
        otherRows.Should().ContainSingle();
        otherRows[0].RevokedAt.Should().BeNull();
        otherRows[0].IsActive.Should().BeTrue();
    }

    private async Task ResetAndMigrateAsync()
    {
        NpgsqlConnection.ClearAllPools();
        await using var db = new IdentityDbContext(_options);
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    private RefreshTokenCommandHandler CreateHandler(
        IdentityDbContext db,
        IRefreshTokenRepository repository,
        ILogger<RefreshTokenCommandHandler>? logger = null)
    {
        var options = Options.Create(_jwtSettings);
        return new RefreshTokenCommandHandler(
            new UserRepository(db),
            repository,
            new JwtTokenService(options),
            options,
            logger ?? NullLogger<RefreshTokenCommandHandler>.Instance);
    }

    private static bool IsSha256Hex(string value) =>
        value.Length == RefreshToken.HashLength
        && value.All(character => char.IsAsciiHexDigit(character) && !char.IsUpper(character));

    private sealed class ReadGateRepository(RefreshTokenRepository inner, Func<Task> afterRead) : IRefreshTokenRepository
    {
        public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        {
            var found = await inner.GetByTokenAsync(token, ct);
            await afterRead();
            return found;
        }

        public Task<IReadOnlyList<RefreshToken>> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default) =>
            inner.GetActiveByUserIdAsync(userId, ct);

        public Task AddAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
            inner.AddAsync(refreshToken, ct);

        public void Stage(RefreshToken refreshToken) => inner.Stage(refreshToken);

        public Task UpdateAsync(RefreshToken refreshToken, CancellationToken ct = default) =>
            inner.UpdateAsync(refreshToken, ct);

        public Task RevokeAllByUserIdAsync(Guid userId, CancellationToken ct = default) =>
            inner.RevokeAllByUserIdAsync(userId, ct);

        public Task<bool> TryRotateAsync(
            Guid activeTokenId,
            RefreshToken replacement,
            string replacementPlaintext,
            CancellationToken ct = default) =>
            inner.TryRotateAsync(activeTokenId, replacement, replacementPlaintext, ct);
    }

    private sealed class CollectingLogger : ILogger<RefreshTokenCommandHandler>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

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
    }
}
