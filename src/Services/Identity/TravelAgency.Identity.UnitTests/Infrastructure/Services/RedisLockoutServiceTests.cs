using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Infrastructure.Services;

namespace TravelAgency.Identity.UnitTests.Infrastructure.Services;

/// <summary>
/// AUDIT-002: RedisLockoutService correctly records failed attempts, returns lockout status,
/// and clears lockout via Reset. Uses mocked IDatabase for unit testing.
/// </summary>
public class RedisLockoutServiceTests
{
    private readonly Mock<IDatabase> _dbMock = new();
    private readonly Mock<IConnectionMultiplexer> _redisMock = new();
    private readonly Mock<ILogger<RedisLockoutService>> _loggerMock = new();
    private readonly LockoutSettings _settings = new()
    {
        LockoutThreshold = 5,
        LockoutDurationMinutes = 15
    };

    public RedisLockoutServiceTests()
    {
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object?>()))
            .Returns(_dbMock.Object);
    }

    private RedisLockoutService CreateSut() =>
        new(_redisMock.Object, Options.Create(_settings), _loggerMock.Object);

    [Fact]
    public void IsLockedOut_WhenNoLockoutRecord_ReturnsFalse()
    {
        _dbMock.Setup(d => d.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns(RedisValue.Null);

        var sut = CreateSut();

        var result = sut.IsLockedOut("user@example.com");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutUntilInFuture_ReturnsTrue()
    {
        var futureUnix = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();
        _dbMock.Setup(d => d.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns((RedisValue)futureUnix.ToString());

        var sut = CreateSut();

        var result = sut.IsLockedOut("user@example.com");

        result.Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_WhenLockoutUntilInPast_ReturnsFalse()
    {
        var pastUnix = DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds();
        _dbMock.Setup(d => d.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns((RedisValue)pastUnix.ToString());

        var sut = CreateSut();

        var result = sut.IsLockedOut("user@example.com");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_WhenHashGetReturnsInvalidValue_ReturnsFalse()
    {
        _dbMock.Setup(d => d.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns((RedisValue)"not-a-number");

        var sut = CreateSut();

        var result = sut.IsLockedOut("user@example.com");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_NormalizesEmailToLowercase()
    {
        RedisKey? capturedKey = null;
        _dbMock.Setup(d => d.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, RedisValue, CommandFlags>((key, _, _) => capturedKey = key)
            .Returns(RedisValue.Null);

        var sut = CreateSut();
        sut.IsLockedOut("User@Example.COM");

        capturedKey.Should().NotBeNull();
        capturedKey!.Value.ToString().Should().Contain("user@example.com");
    }

    [Fact]
    public void RecordFailedAttempt_CallsScriptEvaluate()
    {
        var sut = CreateSut();

        sut.RecordFailedAttempt("user@example.com");

        _dbMock.Verify(d => d.ScriptEvaluate(
            It.IsAny<string>(),
            It.IsAny<RedisKey[]>(),
            It.IsAny<RedisValue[]>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public void ResetFailedAttempts_CallsKeyDelete()
    {
        var sut = CreateSut();

        sut.ResetFailedAttempts("user@example.com");

        _dbMock.Verify(d => d.KeyDelete(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public void ResetFailedAttempts_DeletesLockoutKey()
    {
        RedisKey? deletedKey = null;
        _dbMock.Setup(d => d.KeyDelete(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Callback<RedisKey, CommandFlags>((key, _) => deletedKey = key)
            .Returns(true);

        var sut = CreateSut();
        sut.ResetFailedAttempts("user@example.com");

        deletedKey.Should().NotBeNull();
        deletedKey!.Value.ToString().Should().Contain("user@example.com");
    }

    [Fact]
    public void IsLockedOut_WhenRedisThrows_ReturnsTrue_FailClosed()
    {
        _dbMock.Setup(d => d.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unavailable"));

        var sut = CreateSut();

        var result = sut.IsLockedOut("user@example.com");

        result.Should().BeTrue();
    }

    [Fact]
    public void RecordFailedAttempt_WhenRedisThrows_DoesNotThrow()
    {
        _dbMock.Setup(d => d.ScriptEvaluate(
                It.IsAny<string>(),
                It.IsAny<RedisKey[]>(),
                It.IsAny<RedisValue[]>(),
                It.IsAny<CommandFlags>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unavailable"));

        var sut = CreateSut();

        var act = () => sut.RecordFailedAttempt("user@example.com");

        act.Should().NotThrow();
    }

    [Fact]
    public void ResetFailedAttempts_WhenRedisThrows_DoesNotThrow()
    {
        _dbMock.Setup(d => d.KeyDelete(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .Throws(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Redis unavailable"));

        var sut = CreateSut();

        var act = () => sut.ResetFailedAttempts("user@example.com");

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetKey_WhenEmailIsNullOrWhitespace_ThrowsArgumentException(string? email)
    {
        _dbMock.Setup(d => d.HashGet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns(RedisValue.Null);

        var sut = CreateSut();

        var act = () => sut.IsLockedOut(email!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("email")
            .WithMessage("*cannot be null or whitespace*");
    }
}
