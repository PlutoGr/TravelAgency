using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Chat.Infrastructure.Persistence;

namespace TravelAgency.Chat.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    /// <summary>
    /// Applies pending EF Core migrations for the Chat database.
    /// Call from API Program.cs: app.UseChatMigrations();
    /// Runs when ASPNETCORE_RUN_MIGRATIONS=true (env or config) or when env.IsDevelopment().
    /// </summary>
    public static IApplicationBuilder UseChatMigrations(this IApplicationBuilder app)
    {
        var runMigrations = string.Equals(
            Environment.GetEnvironmentVariable("ASPNETCORE_RUN_MIGRATIONS"),
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!runMigrations)
        {
            var config = app.ApplicationServices.GetService<IConfiguration>();
            runMigrations = string.Equals(
                config?["ASPNETCORE_RUN_MIGRATIONS"],
                "true",
                StringComparison.OrdinalIgnoreCase);
        }

        if (!runMigrations)
        {
            using var scope = app.ApplicationServices.CreateScope();
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
            if (!env.IsDevelopment())
                return app;
        }

        using var migrateScope = app.ApplicationServices.CreateScope();
        var db = migrateScope.ServiceProvider.GetRequiredService<ChatDbContext>();
        db.Database.Migrate();
        return app;
    }
}
