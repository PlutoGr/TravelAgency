using System.Globalization;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

/// <summary>
/// Календарный день вылета. В query берётся дата, как она написана:
/// время и часовой пояс клиента день не сдвигают.
/// </summary>
public static class DepartureCalendarDate
{
    public static bool TryParse(string? text, out DateOnly date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var value = text.Trim();
        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        if (value.Length > 10
            && value[10] == 'T'
            && DateOnly.TryParseExact(value.AsSpan(0, 10), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        return false;
    }

    /// <summary>Начало календарного дня в UTC, Kind = Utc, чтобы параметр прошёл в timestamptz.</summary>
    public static DateTime UtcStart(DateOnly day) =>
        DateTime.SpecifyKind(day.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

    /// <summary>Исключительная граница следующего дня: сам день вылета входит целиком.</summary>
    public static DateTime UtcEndExclusive(DateOnly day) => UtcStart(day.AddDays(1));
}
