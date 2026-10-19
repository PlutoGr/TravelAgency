using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Domain;

/// <summary>
/// Цена «от» и ближайшая дата считаются по дням вылета не раньше сегодня.
/// Сегодня входит: вылет в этот день ещё показывается.
/// </summary>
public static class PublicCatalogPricing
{
    public static bool IsFutureDeparture(DateOnly departureDate, DateOnly today) => departureDate >= today;

    public static decimal? PriceFrom(IEnumerable<TourOffer> offers, DateOnly today)
    {
        decimal? min = null;
        foreach (var offer in offers)
        {
            if (!IsFutureDeparture(offer.DepartureDate, today))
                continue;

            if (min is null || offer.PricePerPerson < min)
                min = offer.PricePerPerson;
        }

        return min;
    }

    /// <summary>Валюта самого дешёвого будущего выезда. При равной цене берётся более ранний день.</summary>
    public static string? CurrencyOfPriceFrom(IEnumerable<TourOffer> offers, DateOnly today)
    {
        TourOffer? best = null;
        foreach (var offer in offers)
        {
            if (!IsFutureDeparture(offer.DepartureDate, today))
                continue;

            if (best is null
                || offer.PricePerPerson < best.PricePerPerson
                || (offer.PricePerPerson == best.PricePerPerson && offer.DepartureDate < best.DepartureDate)
                || (offer.PricePerPerson == best.PricePerPerson
                    && offer.DepartureDate == best.DepartureDate
                    && offer.ValidFrom < best.ValidFrom))
            {
                best = offer;
            }
        }

        return best?.Currency;
    }

    public static DateOnly? NearestDeparture(IEnumerable<TourOffer> offers, DateOnly today)
    {
        DateOnly? nearest = null;
        foreach (var offer in offers)
        {
            if (!IsFutureDeparture(offer.DepartureDate, today))
                continue;

            if (nearest is null || offer.DepartureDate < nearest)
                nearest = offer.DepartureDate;
        }

        return nearest;
    }

    public static IReadOnlyList<TourOffer> FutureOffers(IEnumerable<TourOffer> offers, DateOnly today) =>
        offers.Where(offer => IsFutureDeparture(offer.DepartureDate, today))
            .OrderBy(offer => offer.DepartureDate)
            .ThenBy(offer => offer.ValidFrom)
            .ToList();
}
