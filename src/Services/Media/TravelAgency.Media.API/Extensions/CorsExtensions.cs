namespace TravelAgency.Media.API.Extensions;

public static class CorsExtensions
{
    private const string CorsSection = "Cors";
    private const string AllowedOriginsKey = "AllowedOrigins";

    /// <summary>
    /// Adds CORS with whitelist of allowed origins from configuration.
    /// When Cors:AllowedOrigins is empty, uses safe default (localhost) — never AllowAnyOrigin.
    /// </summary>
    public static IServiceCollection AddMediaCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection(CorsSection).GetSection(AllowedOriginsKey).Get<string[]>();
        if (origins is null or { Length: 0 })
            origins = ["http://localhost:3000", "http://localhost:5000"];

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

    public static IApplicationBuilder UseMediaCors(this IApplicationBuilder app)
    {
        app.UseCors();
        return app;
    }
}
