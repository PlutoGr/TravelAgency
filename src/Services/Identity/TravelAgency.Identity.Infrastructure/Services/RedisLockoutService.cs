using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;

namespace TravelAgency.Identity.Infrastructure.Services;

/// <summary>
/// Redis-backed distributed lockout service. Shares lockout state across Identity instances.
/// </summary>
public sealed class RedisLockoutService : ILockoutService
{
    private const string KeyPrefix = "lockout:";

    private static readonly string RecordFailedAttemptScript = """
        local key = KEYS[1]
        local threshold = tonumber(ARGV[1])
        local duration_sec = tonumber(ARGV[2])
        local now = tonumber(ARGV[3])

        local c = redis.call('HGET', key, 'c')
        local u = redis.call('HGET', key, 'u')

        local count = (c and tonumber(c)) or 0
        local lockout_until = (u and tonumber(u)) or 0

        if lockout_until > 0 and now < lockout_until then
            return count
        end

        if lockout_until > 0 and now >= lockout_until then
            count = 0
        end

        count = count + 1
        redis.call('HSET', key, 'c', count)
        if count >= threshold then
            redis.call('HSET', key, 'u', now + duration_sec)
            redis.call('EXPIRE', key, duration_sec + 60)
        else
            redis.call('EXPIRE', key, 3600)
        end
        return count
        """;

    private readonly IConnectionMultiplexer _redis;
    private readonly LockoutSettings _settings;
    private readonly ILogger<RedisLockoutService> _logger;

    public RedisLockoutService(
        IConnectionMultiplexer redis,
        IOptions<LockoutSettings> settings,
        ILogger<RedisLockoutService> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsLockedOut(string email)
    {
        try
        {
            var key = GetKey(email);
            var db = _redis.GetDatabase();
            var lockoutUntil = db.HashGet(key, "u");

            if (lockoutUntil.IsNullOrEmpty)
                return false;

            if (!long.TryParse(lockoutUntil.ToString(), out var unixSeconds))
                return false;

            return DateTimeOffset.UtcNow.ToUnixTimeSeconds() < unixSeconds;
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis unavailable while checking lockout for {Email}. Failing closed (treating as locked out).", email);
            return true;
        }
    }

    /// <inheritdoc />
    public void RecordFailedAttempt(string email)
    {
        try
        {
            var key = GetKey(email);
            var db = _redis.GetDatabase();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var durationSec = (int)TimeSpan.FromMinutes(_settings.LockoutDurationMinutes).TotalSeconds;

            db.ScriptEvaluate(
                RecordFailedAttemptScript,
                [key],
                [(RedisValue)_settings.LockoutThreshold, (RedisValue)durationSec, (RedisValue)now]);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis unavailable while recording failed attempt for {Email}. Failing closed (no attempt recorded).", email);
        }
    }

    /// <inheritdoc />
    public void ResetFailedAttempts(string email)
    {
        try
        {
            var key = GetKey(email);
            var db = _redis.GetDatabase();
            db.KeyDelete(key);
        }
        catch (RedisException ex)
        {
            _logger.LogError(ex, "Redis unavailable while resetting failed attempts for {Email}. Failing closed (lockout state unchanged).", email);
        }
    }

    private static string GetKey(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();
        return $"{KeyPrefix}{normalized}";
    }
}
