using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Media.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddMediaCors(this IServiceCollection services, IConfiguration configuration)
        => services.AddSharedCors(configuration, ["http://localhost:3000", "http://localhost:5173"]);

    public static IApplicationBuilder UseMediaCors(this IApplicationBuilder app)
        => app.UseSharedCors();
}
