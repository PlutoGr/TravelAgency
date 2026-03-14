using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using TravelAgency.Identity.API.Extensions;
using TravelAgency.Identity.API.Middleware;
using TravelAgency.Identity.Infrastructure.GrpcServices;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddIdentitySerilog();

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
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                Window = TimeSpan.FromSeconds(60),
                SegmentsPerWindow = 6,
                PermitLimit = 5,
                QueueLimit = 0
            }));
});

var app = builder.Build();

app.UseIdentityMigrations();
app.UseIdentityCors();

app.UseSerilogRequestLogging();
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
app.MapGrpcService<IdentityGrpcService>();
app.MapIdentityHealthChecks();

app.Run();
