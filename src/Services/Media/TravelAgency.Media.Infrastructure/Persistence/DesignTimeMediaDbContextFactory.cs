using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TravelAgency.Media.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations. Used by dotnet ef commands.
/// </summary>
internal sealed class DesignTimeMediaDbContextFactory : IDesignTimeDbContextFactory<MediaDbContext>
{
    public MediaDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("MediaDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:MediaDb is required for migrations. Set ConnectionStrings__MediaDb.");

        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new MediaDbContext(options);
    }
}
