using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Identity.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddIdentityCors(this IServiceCollection services, IConfiguration configuration)
        => services.AddSharedCors(configuration, ["http://localhost:3000", "http://localhost:5173"]);

    public static IApplicationBuilder UseIdentityCors(this IApplicationBuilder app)
        => app.UseSharedCors();
}
