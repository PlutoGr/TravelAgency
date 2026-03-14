namespace TravelAgency.Media.API.Extensions;

public static class CorsExtensions
{
    private const string PolicyName = "MediaCorsPolicy";

    public static IServiceCollection AddMediaCors(this IServiceCollection services, IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, builder =>
            {
                if (origins.Length > 0)
                    builder.WithOrigins(origins)
                           .AllowAnyHeader()
                           .AllowAnyMethod();
                else
                    builder.AllowAnyOrigin()
                           .AllowAnyHeader()
                           .AllowAnyMethod();
            });
        });

        return services;
    }

    public static IApplicationBuilder UseMediaCors(this IApplicationBuilder app)
    {
        app.UseCors(PolicyName);
        return app;
    }
}
