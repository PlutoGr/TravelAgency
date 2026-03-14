using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TravelAgency.Chat.Infrastructure.Persistence;

namespace TravelAgency.Chat.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddChatHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var builder = services.AddHealthChecks()
            .AddDbContextCheck<ChatDbContext>("chat-db");

        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            builder.AddRedis(redisConnection, name: "redis", tags: ["redis"]);
        }

        return services;
    }

    public static IEndpointRouteBuilder MapChatHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/ready");

        return endpoints;
    }
}
