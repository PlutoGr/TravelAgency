using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using System.Net;
using TravelAgency.Gateway.Extensions;
using TravelAgency.Gateway.Middleware;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

// Serilog
builder.Host.AddGatewaySerilog();

// Services
builder.Services.AddGatewayAuthentication(configuration);
builder.Services.AddGatewayAuthorization();
builder.Services.AddGatewayYarp(configuration);
builder.Services.AddGatewayRateLimiting(configuration);
builder.Services.AddGatewayCors(configuration);
builder.Services.AddGatewayHealthChecks(configuration);
builder.Services.AddGatewayTracing(configuration, builder.Environment);

var app = builder.Build();

// Middleware pipeline (order matters!)
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
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "no-referrer");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    await next();
});
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors(CorsExtensions.GatewayCorsPolicyName);
app.UseGatewayRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapGatewayHealthChecks();
app.MapGatewayYarp();

app.Run();

public partial class Program { }
