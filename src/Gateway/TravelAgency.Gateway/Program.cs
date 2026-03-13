using AspNetCoreRateLimit;
using Serilog;
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
builder.Services.AddGatewayTracing();

var app = builder.Build();

// Middleware pipeline (order matters!)
app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
app.UseCors(CorsExtensions.GatewayCorsPolicyName);
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapGatewayHealthChecks();
app.MapGatewayYarp();

app.Run();
