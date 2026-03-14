namespace TravelAgency.Booking.API.Extensions;

public static class InfrastructureExtensions
{
    public static IServiceCollection AddBookingInfrastructure(this IServiceCollection services, IConfiguration configuration)
        => TravelAgency.Booking.Infrastructure.DependencyInjection.AddBookingInfrastructure(services, configuration);

    public static IApplicationBuilder UseBookingMigrations(this IApplicationBuilder app)
        => TravelAgency.Booking.Infrastructure.DependencyInjection.UseBookingMigrations(app);
}
