using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TravelAgency.Booking.Infrastructure.Persistence;

namespace TravelAgency.Booking.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddBookingHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<BookingDbContext>("booking-db");

        return services;
    }

    public static IEndpointRouteBuilder MapBookingHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        endpoints.MapHealthChecks("/health/ready");

        return endpoints;
    }
}
