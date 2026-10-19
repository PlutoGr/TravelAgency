using Microsoft.Extensions.Options;
using TravelAgency.Catalog.Application.Time;

namespace TravelAgency.Catalog.Infrastructure.Time;

public sealed class CatalogClock : ICatalogClock
{
    private readonly TimeProvider _time;
    private readonly TimeZoneInfo _zone;

    public CatalogClock(TimeProvider time, IOptions<CatalogOptions> options)
    {
        _time = time;
        _zone = options.Value.ResolveZone();
    }

    public TimeZoneInfo DefaultTimeZone => _zone;

    public DateOnly Today()
    {
        var local = TimeZoneInfo.ConvertTime(_time.GetUtcNow(), _zone);
        return DateOnly.FromDateTime(local.DateTime);
    }
}
