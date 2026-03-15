using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Catalog.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSharedMediatRBehaviors();

        return services;
    }
}
