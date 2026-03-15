using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Infrastructure.Behaviors;
using TravelAgency.Shared.Infrastructure.GrpcServices;
using TravelAgency.Shared.Infrastructure.Services;

namespace TravelAgency.Shared.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering shared infrastructure services.
/// </summary>
public static class SharedInfrastructureExtensions
{
    /// <summary>
    /// Registers the shared <see cref="IGrpcAuthCallOptionsFactory"/> for gRPC service-to-service calls.
    /// Call before registering gRPC clients that make outgoing calls.
    /// </summary>
    public static IServiceCollection AddGrpcAuthCallOptionsFactory(this IServiceCollection services)
    {
        services.AddScoped<IGrpcAuthCallOptionsFactory, GrpcAuthCallOptionsFactory>();
        return services;
    }

    /// <summary>
    /// Adds shared MediatR pipeline behaviors (Logging, Validation).
    /// Call after AddMediatR and AddValidatorsFromAssembly.
    /// </summary>
    public static IServiceCollection AddSharedMediatRBehaviors(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        return services;
    }

    /// <summary>
    /// Registers the shared <see cref="CurrentUserService"/> implementation.
    /// Call AddHttpContextAccessor before or after this.
    /// </summary>
    public static IServiceCollection AddCurrentUserService(this IServiceCollection services)
    {
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }
}
