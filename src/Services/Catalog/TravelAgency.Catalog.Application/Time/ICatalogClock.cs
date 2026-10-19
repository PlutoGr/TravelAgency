namespace TravelAgency.Catalog.Application.Time;

/// <summary>
/// «Сегодня» каталога. В тестах подменяется вместе с TimeProvider.
/// </summary>
public interface ICatalogClock
{
    DateOnly Today();

    TimeZoneInfo DefaultTimeZone { get; }
}
