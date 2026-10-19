using TravelAgency.Catalog.Application.Time;

namespace TravelAgency.Catalog.UnitTests.Time;

internal sealed class TestCatalogClock(DateOnly? today = null, string zoneId = CatalogOptions.DefaultTimeZoneId) : ICatalogClock
{
    public DateOnly Today() => today ?? DateOnly.FromDateTime(DateTime.UtcNow);

    public TimeZoneInfo DefaultTimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
}
