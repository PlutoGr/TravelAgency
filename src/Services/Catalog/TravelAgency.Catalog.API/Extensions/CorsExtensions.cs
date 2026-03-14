namespace TravelAgency.Catalog.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCatalogCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
                policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
        });
        return services;
    }
}
