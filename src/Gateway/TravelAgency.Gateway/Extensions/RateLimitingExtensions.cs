using Microsoft.AspNetCore.RateLimiting;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using TravelAgency.Gateway.Configuration;
using TravelAgency.Gateway.RateLimiting;

namespace TravelAgency.Gateway.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnection = configuration.GetConnectionString("Redis");
        var useRedis = !string.IsNullOrWhiteSpace(redisConnection);

        if (useRedis)
        {
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection!));
        }

        services.Configure<AuthRouteSettings>(configuration.GetSection(AuthRouteSettings.SectionName));

        var globalLimit = configuration.GetValue<int>("RateLimiting:Global:Limit", 200);
        var globalWindow = TimeSpan.FromMinutes(1);
        var authLimit = configuration.GetValue<int>("RateLimiting:Auth:Limit", 5);
        var authWindow = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:Auth:PeriodSeconds", 1));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("fixed", opt =>
            {
                opt.PermitLimit = configuration.GetValue<int>("RateLimiting:General:Limit", 100);
                opt.Window = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:General:PeriodSeconds", 1));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("auth", opt =>
            {
                opt.PermitLimit = authLimit;
                opt.Window = authWindow;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var path = context.Request.Path.Value ?? "";
                var authSettings = context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AuthRouteSettings>>().Value;
                var isAuth = authSettings.TokenResponsePaths.Any(p => path.StartsWith(p.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
                    || path.Equals(authSettings.LogoutPath, StringComparison.OrdinalIgnoreCase);

                var partitionKey = context.Connection.RemoteIpAddress?.ToString()
                    ?? context.Request.Headers.Host.ToString()
                    ?? "unknown";
                var compositeKey = $"{partitionKey}:{(isAuth ? "auth" : "global")}";
                var (limit, window) = isAuth ? (authLimit, authWindow) : (globalLimit, globalWindow);

                if (useRedis)
                {
                    return RateLimitPartition.Get(compositeKey, key =>
                    {
                        var redis = context.RequestServices.GetRequiredService<IConnectionMultiplexer>();
                        return new RedisFixedWindowRateLimiter(redis, key, limit, window);
                    });
                }
                return RateLimitPartition.GetFixedWindowLimiter(
                    compositeKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = limit,
                        Window = window
                    });
            });
        });

        return services;
    }

    public static IApplicationBuilder UseGatewayRateLimiting(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}
