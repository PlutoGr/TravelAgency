using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace TravelAgency.Shared.Infrastructure.Seeding;

/// <summary>
/// Демо-данные включаются только там, где сидер уже был включён:
/// Development, либо явный флаг на Staging. Production не сидируется даже с флагом.
/// </summary>
public static class DemoSeedGate
{
    public const string DemoCatalogKey = "Seeding:DemoCatalog";
    public const string SeedDataKey = "ASPNETCORE_SEED_DATA";
    public const string TestUserPasswordKey = "Seeding:TestUserPassword";

    public static bool ShouldSeed(IHostEnvironment environment, IConfiguration configuration, bool includeDemoCatalogFlag)
    {
        if (environment.IsProduction())
            return false;

        if (environment.IsDevelopment())
            return true;

        if (string.Equals(configuration[SeedDataKey], "true", StringComparison.OrdinalIgnoreCase))
            return true;

        return includeDemoCatalogFlag && configuration.GetValue<bool>(DemoCatalogKey);
    }
}
