using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace TravelAgency.Gateway.RateLimiting;

/// <summary>
/// Redis-backed fixed-window rate limiter for distributed deployments.
/// Uses INCR/EXPIRE for atomic per-partition counting across gateway instances.
/// </summary>
internal sealed class RedisFixedWindowRateLimiter : RateLimiter
{
    private readonly IConnectionMultiplexer _redis;
    private readonly string _keyPrefix;
    private readonly int _permitLimit;
    private readonly TimeSpan _window;
    private static readonly string LuaScript = """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
            redis.call('EXPIRE', KEYS[1], ARGV[1])
        end
        return current
        """;

    public RedisFixedWindowRateLimiter(
        IConnectionMultiplexer redis,
        string partitionKey,
        int permitLimit,
        TimeSpan window)
    {
        _redis = redis;
        var safeKey = string.Join("_", partitionKey.Split([':', '.', ' '], StringSplitOptions.RemoveEmptyEntries));
        _keyPrefix = $"ratelimit:gateway:{safeKey}";
        _permitLimit = permitLimit;
        _window = window;
    }

    protected override ValueTask<RateLimitLease> AcquireAsyncCore(int permitCount, CancellationToken cancellationToken)
    {
        var lease = TryAcquireCore(permitCount);
        return new ValueTask<RateLimitLease>(lease);
    }

    protected override RateLimitLease AttemptAcquireCore(int permitCount)
        => TryAcquireCore(permitCount);

    private RateLimitLease TryAcquireCore(int permitCount)
    {
        try
        {
            var db = _redis.GetDatabase();
            var windowStart = (DateTimeOffset.UtcNow.ToUnixTimeSeconds() / (long)_window.TotalSeconds) * (long)_window.TotalSeconds;
            var key = $"{_keyPrefix}:{windowStart}";
            var result = db.ScriptEvaluate(LuaScript, [key], [(int)_window.TotalSeconds]);
            var current = result.IsNull ? 0L : (long)result;
            var allowed = current <= _permitLimit;
            return allowed
                ? new RedisRateLimitLease(permitCount, null)
                : new RedisRateLimitLease(0, _window);
        }
        catch (RedisConnectionException)
        {
            // Redis unavailable: allow request (fail open for availability)
            return new RedisRateLimitLease(permitCount, null);
        }
    }

    public override TimeSpan? IdleDuration => null;

    public override RateLimiterStatistics? GetStatistics() => null;

    private sealed class RedisRateLimitLease : RateLimitLease
    {
        private readonly int _permitCount;
        private readonly TimeSpan? _retryAfter;

        public RedisRateLimitLease(int permitCount, TimeSpan? retryAfter)
        {
            _permitCount = permitCount;
            _retryAfter = retryAfter;
        }

        public override bool IsAcquired => _permitCount > 0;
        protected override void Dispose(bool disposing) { }
        public override IEnumerable<string> MetadataNames => [];
        public override bool TryGetMetadata(string metadataName, out object? metadata)
        {
            if (metadataName == "RateLimit.RetryAfter" && _retryAfter.HasValue)
            {
                metadata = _retryAfter.Value;
                return true;
            }
            metadata = null;
            return false;
        }
    }
}
