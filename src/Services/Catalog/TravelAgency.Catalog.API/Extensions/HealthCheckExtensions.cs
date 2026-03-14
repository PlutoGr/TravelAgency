namespace TravelAgency.Catalog.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddCatalogHealthChecks(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks();
        return services;
    }
}
