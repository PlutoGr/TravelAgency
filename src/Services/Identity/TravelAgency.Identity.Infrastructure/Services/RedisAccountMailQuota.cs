using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TravelAgency.Identity.Application.Abstractions;

namespace TravelAgency.Identity.Infrastructure.Services;

public sealed class RedisAccountMailQuota(IConnectionMultiplexer redis, ILogger<RedisAccountMailQuota> logger)
    : IAccountMailQuota
{
    private const int Limit = 3;
    private const int WindowSeconds = 3600;

    private static readonly string ReserveScript = """
        local key = KEYS[1]
        local now = tonumber(ARGV[1])
        local window_ms = tonumber(ARGV[2])
        local expire_sec = tonumber(ARGV[3])
        local limit = tonumber(ARGV[4])
        local member = ARGV[5]
        redis.call('ZREMRANGEBYSCORE', key, 0, now - window_ms)
        local count = redis.call('ZCARD', key)
        if count >= limit then
            return 0
        end
        redis.call('ZADD', key, now, member)
        redis.call('EXPIRE', key, expire_sec)
        return 1
        """;

    private static readonly string ReleaseScript = """
        local key = KEYS[1]
        local latest = redis.call('ZREVRANGE', key, 0, 0)
        if latest[1] then
            redis.call('ZREM', key, latest[1])
        end
        if redis.call('ZCARD', key) == 0 then
            redis.call('DEL', key)
        end
        return 1
        """;

    public async Task<AccountMailSlot> TryReserveAsync(
        string bucket,
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var reserved = (int)await redis.GetDatabase().ScriptEvaluateAsync(
                ReserveScript,
                [Key(bucket, normalizedEmail)],
                [now, WindowSeconds * 1000, WindowSeconds, Limit, Guid.NewGuid().ToString("N")]);
            return reserved == 1 ? AccountMailSlot.Reserved : AccountMailSlot.Exhausted;
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Счётчик писем недоступен");
            return AccountMailSlot.StoreUnavailable;
        }
    }

    public async Task ReleaseAsync(string bucket, string normalizedEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            await redis.GetDatabase().ScriptEvaluateAsync(ReleaseScript, [Key(bucket, normalizedEmail)]);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "Не удалось освободить слот письма");
        }
    }

    public static string Key(string bucket, string normalizedEmail) =>
        $"account-mail:{bucket}:{normalizedEmail}";
}
