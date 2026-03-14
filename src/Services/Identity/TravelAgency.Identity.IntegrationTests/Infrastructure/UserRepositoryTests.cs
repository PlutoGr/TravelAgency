using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Infrastructure.Persistence;
using TravelAgency.Identity.Infrastructure.Repositories;

namespace TravelAgency.Identity.IntegrationTests.Infrastructure;

public class UserRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IdentityDbContext _dbContext;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new IdentityDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repository = new UserRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_AddsUserToDatabase()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);

        await _repository.AddAsync(user);

        var found = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        found.Should().NotBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        var user = User.Create("test@example.com", "hash", "John", "Doe", null);
        await _repository.AddAsync(user);

        var result = await _repository.GetByIdAsync(user.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_WhenUserNotExists_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserExists_ReturnsUser()
    {
        var user = User.Create("find@example.com", "hash", "John", "Doe", null);
        await _repository.AddAsync(user);

        var result = await _repository.GetByEmailAsync("find@example.com");

        result.Should().NotBeNull();
        result!.Email.Should().Be("find@example.com");
    }

    [Fact]
    public async Task GetByEmailAsync_WhenUserNotExists_ReturnsNull()
    {
        var result = await _repository.GetByEmailAsync("notfound@example.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_WhenUserExists_ReturnsTrue()
    {
        var user = User.Create("exists@example.com", "hash", "John", "Doe", null);
        await _repository.AddAsync(user);

        var result = await _repository.ExistsAsync("exists@example.com");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_WhenUserNotExists_ReturnsFalse()
    {
        var result = await _repository.ExistsAsync("doesnotexist@example.com");

        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesUserInDatabase()
    {
        var user = User.Create("update@example.com", "hash", "Old", "Name", null);
        await _repository.AddAsync(user);

        user.UpdateProfile("New", "Surname", "+123");
        await _repository.UpdateAsync(user);

        var updated = await _repository.GetByIdAsync(user.Id);
        updated!.FirstName.Should().Be("New");
        updated.LastName.Should().Be("Surname");
        updated.Phone.Should().Be("+123");
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseSensitive()
    {
        var user = User.Create("case@example.com", "hash", "J", "D", null);
        await _repository.AddAsync(user);

        var result = await _repository.GetByEmailAsync("CASE@EXAMPLE.COM");

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_WithDuplicateEmail_ThrowsException()
    {
        var user1 = User.Create("dup@example.com", "hash", "A", "B", null);
        var user2 = User.Create("dup@example.com", "hash2", "C", "D", null);
        await _repository.AddAsync(user1);

        var act = async () => await _repository.AddAsync(user2);

        await act.Should().ThrowAsync<Exception>();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
