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
    /// Runs when ASPNETCORE_RUN_MIGRATIONS=true in configuration or when env.IsDevelopment().
    /// The host copies the process variable into configuration.
    /// </summary>
    public static IApplicationBuilder UseChatMigrations(this IApplicationBuilder app)
    {
        var configuration = app.ApplicationServices.GetRequiredService<IConfiguration>();
        var runMigrations = string.Equals(
            configuration["ASPNETCORE_RUN_MIGRATIONS"],
            "true",
            StringComparison.OrdinalIgnoreCase);

        if (!runMigrations && !app.ApplicationServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
            return app;

        using var migrateScope = app.ApplicationServices.CreateScope();
        var db = migrateScope.ServiceProvider.GetRequiredService<ChatDbContext>();
        db.Database.Migrate();
        return app;
    }
}
