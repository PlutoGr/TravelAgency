using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddIdentityHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddDbContextCheck<IdentityDbContext>();

        return services;
    }

    public static WebApplication MapIdentityHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready");

        return app;
    }
}
