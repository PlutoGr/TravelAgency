using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace TravelAgency.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<Features.Tours.Manage.TourManageStore>();

        return services;
    }
}
