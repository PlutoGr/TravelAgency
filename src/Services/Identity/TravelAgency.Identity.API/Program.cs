using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using TravelAgency.Identity.API.Extensions;
using TravelAgency.Identity.API.Middleware;
using TravelAgency.Identity.Infrastructure.Extensions;
using TravelAgency.Identity.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.Hosting;
using TravelAgency.Shared.Infrastructure.Logging;
using TravelAgency.Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddIdentitySerilog();
builder.UseServiceListenPorts();
Program.ConfigureServices(builder);

var app = builder.Build();

Program.ConfigurePipeline(app);

app.Run();

public partial class Program
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IExceptionMapper, IdentityExceptionMapper>();
        builder.Services.AddControllers();
        builder.Services.AddSingleton<GrpcAuthInterceptor>();
        builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcAuthInterceptor>());
        builder.Services.AddIdentityAuthentication(builder.Configuration);
        builder.Services.AddIdentityAuthorization();
        builder.Services.AddIdentityInfrastructure(builder.Configuration);
        builder.Services.AddIdentityCors(builder.Configuration);
        builder.Services.AddIdentityHealthChecks();
        builder.Services.AddIdentitySwagger();
        builder.Services.AddIdentityTracing();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            var permitLimit = builder.Configuration.GetValue<int?>("RateLimit:PermitLimit")
                ?? (builder.Environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase)
                    ? 1000
                    : 5);
            options.AddPolicy("auth", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        Window = TimeSpan.FromSeconds(60),
                        SegmentsPerWindow = 6,
                        PermitLimit = permitLimit,
                        QueueLimit = 0
                    }));
        });
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseGrpcListenPortGuard();

        // Trust forwarded headers from Gateway so rate limiting uses real client IP
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            KnownIPNetworks =
            {
                new System.Net.IPNetwork(IPAddress.Parse("10.0.0.0"), 8),
                new System.Net.IPNetwork(IPAddress.Parse("172.16.0.0"), 12),
                new System.Net.IPNetwork(IPAddress.Parse("192.168.0.0"), 16)
            }
        });

        app.UseIdentityMigrations();
        app.UseIdentityCors();

        app.UseTravelAgencyRequestLogging();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseIdentitySwagger();
        }

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapGrpcServiceOnGrpcPort<IdentityGrpcService>();
        app.MapIdentityHealthChecks();
    }
}
