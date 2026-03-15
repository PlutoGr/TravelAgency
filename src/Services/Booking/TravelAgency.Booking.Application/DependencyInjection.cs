using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace TravelAgency.Booking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddBookingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
