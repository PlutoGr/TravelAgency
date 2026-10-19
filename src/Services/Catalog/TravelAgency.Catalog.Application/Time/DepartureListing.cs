namespace TravelAgency.Catalog.Application.Time;

/// <summary>
/// Выдача смотрит на DepartureDate. Сегодня и обе границы фильтра включительны.
/// </summary>
public static class DepartureListing
{
    public static bool Includes(DateOnly departureDate, DateOnly today, DateOnly? dateFrom, DateOnly? dateTo)
    {
        if (departureDate < today)
            return false;

        if (dateFrom is DateOnly from && departureDate < from)
            return false;

        if (dateTo is DateOnly to && departureDate > to)
            return false;

        return true;
    }
}
