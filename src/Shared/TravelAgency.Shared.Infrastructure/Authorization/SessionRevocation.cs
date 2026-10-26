using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Shared.Infrastructure.Authorization;

public static class SessionRevocationServiceCollectionExtensions
{
    public static IServiceCollection AddSessionRevocation(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis")
            ?? configuration["Lockout:RedisConnection"];

        if (string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddSingleton<ISessionRevocationStore, NullSessionRevocationStore>();
            services.AddSingleton<ISessionRevocationListener, NullSessionRevocationListener>();
            return services;
        }

        if (!services.Any(descriptor => descriptor.ServiceType == typeof(IConnectionMultiplexer)))
        {
            services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddSingleton<RedisSessionRevocationStore>();
        services.AddSingleton<ISessionRevocationStore>(sp => sp.GetRequiredService<RedisSessionRevocationStore>());
        services.AddSingleton<ISessionRevocationListener>(sp => sp.GetRequiredService<RedisSessionRevocationStore>());
        return services;
    }
}

public sealed class NullSessionRevocationStore : ISessionRevocationStore
{
    public Task SetAsync(Guid userId, long version, bool blocked, TimeSpan ttl, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<SessionRevocationEntry?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<SessionRevocationEntry?>(null);

    public Task PublishAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

public sealed class NullSessionRevocationListener : ISessionRevocationListener
{
    public Task ListenAsync(Action<Guid> onRevoked, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class RedisSessionRevocationStore(IConnectionMultiplexer redis) : ISessionRevocationStore, ISessionRevocationListener
{
    public async Task SetAsync(
        Guid userId,
        long version,
        bool blocked,
        TimeSpan ttl,
        CancellationToken cancellationToken = default)
    {
        await redis.GetDatabase().StringSetAsync(
            SessionRevocationNames.Key(userId),
            SessionRevocationNames.Format(version, blocked),
            ttl);
    }

    public async Task<SessionRevocationEntry?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var value = await redis.GetDatabase().StringGetAsync(SessionRevocationNames.Key(userId));
        if (value.IsNullOrEmpty)
            return null;

        return SessionRevocationNames.TryParse(value.ToString(), out var version, out var blocked)
            ? new SessionRevocationEntry(version, blocked)
            : null;
    }

    public async Task PublishAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await redis.GetSubscriber().PublishAsync(Channel(), userId.ToString("D"));
    }

    public async Task ListenAsync(Action<Guid> onRevoked, CancellationToken cancellationToken)
    {
        var subscriber = redis.GetSubscriber();
        var channel = Channel();
        await subscriber.SubscribeAsync(channel, (_, value) =>
        {
            if (Guid.TryParse(value.ToString(), out var userId))
                onRevoked(userId);
        });

        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            await subscriber.UnsubscribeAsync(channel);
        }
    }

    private static RedisChannel Channel() =>
        new(SessionRevocationNames.Channel, RedisChannel.PatternMode.Literal);
}

public static class SessionRevocationPaths
{
    public static bool IsCredentialExchange(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return Matches(path, "/identity/login")
            || Matches(path, "/identity/register")
            || Matches(path, "/identity/refresh")
            || Matches(path, "/identity/logout")
            || Matches(path, "/api/v1/auth/login")
            || Matches(path, "/api/v1/auth/register")
            || Matches(path, "/api/v1/auth/refresh")
            || Matches(path, "/api/v1/auth/logout");
    }

    private static bool Matches(string path, string prefix) =>
        path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
}

public sealed class SessionRevocationMiddleware(
    RequestDelegate next,
    ILogger<SessionRevocationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ISessionRevocationStore store)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || SessionRevocationPaths.IsCredentialExchange(context.Request.Path.Value))
        {
            await next(context);
            return;
        }

        var userId = ReadUserId(context.User);
        if (userId == Guid.Empty)
        {
            await next(context);
            return;
        }

        SessionRevocationEntry? entry;
        try
        {
            entry = await store.GetAsync(userId, context.RequestAborted);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Не удалось проверить отзыв сессии {UserId}", userId);
            await next(context);
            return;
        }

        if (SessionRevocationNames.Rejects(entry, SessionRevocationNames.ReadVersion(context.User)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }

    public static Guid ReadUserId(System.Security.Claims.ClaimsPrincipal user)
    {
        var value = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sub")?.Value;
        return Guid.TryParse(value, out var userId) ? userId : Guid.Empty;
    }
}
