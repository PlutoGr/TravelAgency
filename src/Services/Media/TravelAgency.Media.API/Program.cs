using Serilog;
using TravelAgency.Media.API.Extensions;
using TravelAgency.Media.API.Middleware;
using TravelAgency.Media.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddMediaSerilog();

builder.Services.AddControllers();
builder.Services.AddMediaAuthentication(builder.Configuration);
builder.Services.AddMediaAuthorization();
builder.Services.AddMediaInfrastructure(builder.Configuration);
builder.Services.AddMediaCors(builder.Configuration);
builder.Services.AddMediaHealthChecks();
builder.Services.AddMediaSwagger();
builder.Services.AddMediaTracing();

var app = builder.Build();

app.UseMediaCors();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseMediaSwagger();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapMediaHealthChecks();

app.Run();

// Make Program class visible for integration tests
public partial class Program { }
