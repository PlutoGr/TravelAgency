using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Repositories;

namespace TravelAgency.Identity.IntegrationTests.Infrastructure;

public class RefreshTokenRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdentityDbContext _dbContext;
    private readonly RefreshTokenRepository _repository;
    private readonly UserRepository _userRepository;

    public RefreshTokenRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new IdentityDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new RefreshTokenRepository(_dbContext);
        _userRepository = new UserRepository(_dbContext);
    }

    private async Task<User> CreateAndSaveUserAsync()
    {
        var user = User.Create("user@example.com", "hash", "J", "D", null);
        await _userRepository.AddAsync(user);
        return user;
    }

    [Fact]
    public async Task AddAsync_AddsTokenToDatabase()
    {
        var user = await CreateAndSaveUserAsync();
        var token = RefreshToken.Create(user.Id, "token123", DateTime.UtcNow.AddDays(7));

        await _repository.AddAsync(token);

        var found = await _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.Id == token.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByTokenAsync_WhenExists_ReturnsToken()
    {
        var user = await CreateAndSaveUserAsync();
        var token = RefreshToken.Create(user.Id, "findme", DateTime.UtcNow.AddDays(7));
        await _repository.AddAsync(token);

        var result = await _repository.GetByTokenAsync("findme");

        result.Should().NotBeNull();
        result!.Token.Should().Be("findme");
    }

    [Fact]
    public async Task GetByTokenAsync_WhenNotExists_ReturnsNull()
    {
        var result = await _repository.GetByTokenAsync("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_ReturnsOnlyActiveTokens()
    {
        var user = await CreateAndSaveUserAsync();
        var activeToken = RefreshToken.Create(user.Id, "active", DateTime.UtcNow.AddDays(7));
        var expiredToken = RefreshToken.Create(user.Id, "expired", DateTime.UtcNow.AddSeconds(-1));
        var revokedToken = RefreshToken.Create(user.Id, "revoked", DateTime.UtcNow.AddDays(7));
        revokedToken.Revoke();

        await _repository.AddAsync(activeToken);
        await _repository.AddAsync(expiredToken);
        await _repository.AddAsync(revokedToken);

        var result = await _repository.GetActiveByUserIdAsync(user.Id);

        result.Should().HaveCount(1);
        result[0].Token.Should().Be("active");
    }

    [Fact]
    public async Task GetActiveByUserIdAsync_WhenNoActiveTokens_ReturnsEmpty()
    {
        var user = await CreateAndSaveUserAsync();
        var expiredToken = RefreshToken.Create(user.Id, "expired", DateTime.UtcNow.AddSeconds(-1));
        await _repository.AddAsync(expiredToken);

        var result = await _repository.GetActiveByUserIdAsync(user.Id);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTokenInDatabase()
    {
        var user = await CreateAndSaveUserAsync();
        var token = RefreshToken.Create(user.Id, "updatable", DateTime.UtcNow.AddDays(7));
        await _repository.AddAsync(token);

        token.Revoke("newToken");
        await _repository.UpdateAsync(token);

        var updated = await _repository.GetByTokenAsync("updatable");
        updated!.IsRevoked.Should().BeTrue();
        updated.ReplacedByToken.Should().Be("newToken");
    }

    [Fact]
    public async Task RevokeAllByUserIdAsync_RevokesAllActiveTokens()
    {
        var user = await CreateAndSaveUserAsync();
        var token1 = RefreshToken.Create(user.Id, "t1", DateTime.UtcNow.AddDays(7));
        var token2 = RefreshToken.Create(user.Id, "t2", DateTime.UtcNow.AddDays(7));
        var expiredToken = RefreshToken.Create(user.Id, "t3", DateTime.UtcNow.AddSeconds(-1));

        await _repository.AddAsync(token1);
        await _repository.AddAsync(token2);
        await _repository.AddAsync(expiredToken);

        await _repository.RevokeAllByUserIdAsync(user.Id);

        var active = await _repository.GetActiveByUserIdAsync(user.Id);
        active.Should().BeEmpty();

        var t1Updated = await _repository.GetByTokenAsync("t1");
        var t2Updated = await _repository.GetByTokenAsync("t2");
        t1Updated!.IsRevoked.Should().BeTrue();
        t2Updated!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllByUserIdAsync_OnlyRevokesTokensForSpecifiedUser()
    {
        var user1 = await CreateAndSaveUserAsync();
        var user2 = User.Create("other@example.com", "hash", "O", "T", null);
        await _userRepository.AddAsync(user2);

        var user1Token = RefreshToken.Create(user1.Id, "user1-token", DateTime.UtcNow.AddDays(7));
        var user2Token = RefreshToken.Create(user2.Id, "user2-token", DateTime.UtcNow.AddDays(7));

        await _repository.AddAsync(user1Token);
        await _repository.AddAsync(user2Token);

        await _repository.RevokeAllByUserIdAsync(user1.Id);

        var user2Active = await _repository.GetActiveByUserIdAsync(user2.Id);
        user2Active.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddAsync_CascadeDeleteFromUser_RemovesToken()
    {
        var user = await CreateAndSaveUserAsync();
        var token = RefreshToken.Create(user.Id, "cascade-token", DateTime.UtcNow.AddDays(7));
        await _repository.AddAsync(token);

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        var found = await _repository.GetByTokenAsync("cascade-token");
        found.Should().BeNull();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
