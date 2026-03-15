using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Catalog.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddCatalogCors(this IServiceCollection services, IConfiguration configuration)
        => services.AddSharedCors(configuration, ["http://localhost:3000", "http://localhost:5173"]);

    public static IApplicationBuilder UseCatalogCors(this IApplicationBuilder app)
        => app.UseSharedCors();
}
