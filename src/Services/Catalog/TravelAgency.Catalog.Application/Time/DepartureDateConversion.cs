namespace TravelAgency.Catalog.Application.Time;

/// <summary>
/// Перевод момента ValidFrom в местный день вылета.
/// Полночь без времени это уже календарный день, его пояс не сдвигает.
/// Момент с временем переводится в пояс города. Города нет в таблице: запасной пояс и строка в лог.
/// </summary>
public static class DepartureDateConversion
{
    public const string UnknownCityMarker = "без пояса";

    public static string UnknownCityLine(Guid? offerId, string? departureCity) =>
        $"DepartureDate: город {UnknownCityMarker}, предложение {offerId}, город \"{departureCity}\"";

    public static string UpdatedLine(int count) =>
        $"DepartureDate: обновлено {count} записей";

    public static DateTime AsUtcTimestamp(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    /// <summary>
    /// День, который ввёл менеджер (время 00:00:00), либо местный день момента вылета.
    /// </summary>
    public static DateOnly ForInput(DateTime value, string? departureCity, TimeZoneInfo defaultZone)
    {
        if (value.TimeOfDay == TimeSpan.Zero)
            return DateOnly.FromDateTime(value);

        return ToLocalDate(AsUtcTimestamp(value), departureCity, defaultZone, log: null);
    }

    public static DateOnly ToLocalDate(
        DateTime validFrom,
        string? departureCity,
        TimeZoneInfo defaultZone,
        ICollection<string>? log,
        Guid? offerId = null)
    {
        var utc = DateTime.SpecifyKind(AsUtcTimestamp(validFrom), DateTimeKind.Utc);
        var zoneId = DepartureCityTimeZones.FindZoneId(departureCity);
        TimeZoneInfo zone;
        if (zoneId is null)
        {
            zone = defaultZone;
            log?.Add(UnknownCityLine(offerId, departureCity));
        }
        else
        {
            zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        }

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, zone);
        return DateOnly.FromDateTime(local);
    }

    public static IReadOnlyList<(Guid OfferId, DateOnly DepartureDate)> ConvertOffers(
        IEnumerable<(Guid OfferId, DateTime ValidFrom, string? DepartureCity)> offers,
        TimeZoneInfo defaultZone,
        ICollection<string> log)
    {
        var converted = new List<(Guid OfferId, DateOnly DepartureDate)>();
        foreach (var offer in offers)
        {
            var date = ToLocalDate(offer.ValidFrom, offer.DepartureCity, defaultZone, log, offer.OfferId);
            converted.Add((offer.OfferId, date));
        }

        log.Add(UpdatedLine(converted.Count));
        return converted;
    }
}
