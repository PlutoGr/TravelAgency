using Serilog;
using TravelAgency.Booking.API.Extensions;
using TravelAgency.Booking.API.Middleware;
using TravelAgency.Booking.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.Hosting;
using TravelAgency.Shared.Infrastructure.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddBookingSerilog();
builder.UseServiceListenPorts();
Program.ConfigureServices(builder);

var app = builder.Build();

Program.ConfigurePipeline(app);

app.Run();

public partial class Program
{
    public static void ConfigureServices(WebApplicationBuilder builder)
    {
        builder.Services.AddSingleton<IExceptionMapper, BookingExceptionMapper>();
        builder.Services.AddControllers();
        builder.Services.AddBookingAuthentication(builder.Configuration);
        builder.Services.AddBookingAuthorization();
        builder.Services.AddBookingInfrastructure(builder.Configuration);
        builder.Services.AddBookingCors(builder.Configuration);
        builder.Services.AddBookingHealthChecks();
        builder.Services.AddBookingSwagger();
        builder.Services.AddBookingTracing();
        builder.Services.AddSingleton<GrpcAuthInterceptor>();
        builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcAuthInterceptor>());
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseGrpcListenPortGuard();
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
        app.MapGrpcServiceOnGrpcPort<BookingGrpcService>();
        app.MapBookingHealthChecks();
    }
}
