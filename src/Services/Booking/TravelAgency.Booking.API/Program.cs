using Serilog;
using TravelAgency.Booking.API.Extensions;
using TravelAgency.Booking.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddBookingSerilog();

builder.Services.AddControllers();
builder.Services.AddBookingAuthentication(builder.Configuration);
builder.Services.AddBookingAuthorization();
builder.Services.AddBookingInfrastructure(builder.Configuration);
builder.Services.AddBookingCors(builder.Configuration);
builder.Services.AddBookingHealthChecks();
builder.Services.AddBookingSwagger();
builder.Services.AddBookingTracing();

var app = builder.Build();

app.UseBookingMigrations();
app.UseBookingCors();

app.UseSerilogRequestLogging();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseBookingSwagger();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapBookingHealthChecks();

app.Run();

public partial class Program { }
