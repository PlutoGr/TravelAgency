using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Media.Infrastructure.Persistence;

namespace TravelAgency.Media.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations at startup when RunMigrations is enabled
    /// (e.g. in Docker or development) so the database is ready.
    /// </summary>
    public static IApplicationBuilder UseMediaMigrations(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var runMigrations = string.Equals(
            configuration["ASPNETCORE_RUN_MIGRATIONS"],
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!runMigrations && !app.ApplicationServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
            return app;

        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        db.Database.Migrate();
        return app;
    }
}
