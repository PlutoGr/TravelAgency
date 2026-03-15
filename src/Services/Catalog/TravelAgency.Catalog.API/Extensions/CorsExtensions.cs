namespace TravelAgency.Catalog.API.Extensions;

public static class CorsExtensions
{
    private const string CorsSection = "Cors";
    private const string AllowedOriginsKey = "AllowedOrigins";

    /// <summary>
    /// Adds CORS with whitelist of allowed origins from configuration (MVP-1 Security: CORS whitelist).
    /// </summary>
    public static IServiceCollection AddCatalogCors(this IServiceCollection services, IConfiguration configuration)
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

    public static IApplicationBuilder UseCatalogCors(this IApplicationBuilder app)
    {
        app.UseCors();
        return app;
    }
}
