using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TravelAgency.Catalog.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core migrations. Used by dotnet ef commands.
/// </summary>
internal sealed class DesignTimeCatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../TravelAgency.Catalog.API"))
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("CatalogDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("ConnectionStrings:CatalogDb is required for migrations. Set ConnectionStrings__CatalogDb environment variable or add it to appsettings.json.");

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new CatalogDbContext(options);
    }
}
