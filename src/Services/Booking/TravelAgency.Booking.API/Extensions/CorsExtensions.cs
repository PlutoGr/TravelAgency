namespace TravelAgency.Booking.API.Extensions;

public static class CorsExtensions
{
    private const string CorsSection = "Cors";
    private const string AllowedOriginsKey = "AllowedOrigins";

    public static IServiceCollection AddBookingCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(CorsSection).GetSection(AllowedOriginsKey).Get<string[]>()
            ?? ["http://localhost:3000", "http://localhost:5000"];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseBookingCors(this IApplicationBuilder app)
    {
        app.UseCors();
        return app;
    }
}
