using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Booking.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddBookingCors(this IServiceCollection services, IConfiguration configuration)
        => services.AddSharedCors(configuration, ["http://localhost:3000", "http://localhost:5173"]);

    public static IApplicationBuilder UseBookingCors(this IApplicationBuilder app)
        => app.UseSharedCors();
}
