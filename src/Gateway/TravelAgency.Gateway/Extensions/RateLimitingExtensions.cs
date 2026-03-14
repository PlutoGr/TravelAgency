using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace TravelAgency.Gateway.Extensions;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
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
                opt.PermitLimit = configuration.GetValue<int>("RateLimiting:Auth:Limit", 5);
                opt.Window = TimeSpan.FromSeconds(configuration.GetValue<int>("RateLimiting:Auth:PeriodSeconds", 1));
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? context.Request.Headers.Host.ToString(),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = configuration.GetValue<int>("RateLimiting:Global:Limit", 200),
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        return services;
    }

    public static IApplicationBuilder UseGatewayRateLimiting(this IApplicationBuilder app)
    {
        app.UseRateLimiter();
        return app;
    }
}
