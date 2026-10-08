using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Domain;

/// <summary>
/// Цена «от» и ближайшая дата считаются только по будущим выездам.
/// Будущий выезд — предложение, у которого ValidFrom строго позже utcNow
/// (то же правило, что в проверке публикации).
/// </summary>
public static class PublicCatalogPricing
{
    public static bool IsFutureDeparture(DateTime validFrom, DateTime utcNow) => validFrom > utcNow;

    public static decimal? PriceFrom(IEnumerable<TourOffer> offers, DateTime utcNow)
    {
        decimal? min = null;
        foreach (var offer in offers)
        {
            if (!IsFutureDeparture(offer.ValidFrom, utcNow))
                continue;

            if (min is null || offer.PricePerPerson < min)
                min = offer.PricePerPerson;
        }

        return min;
    }

    /// <summary>Валюта самого дешёвого будущего выезда. При равной цене берётся более ранний.</summary>
    public static string? CurrencyOfPriceFrom(IEnumerable<TourOffer> offers, DateTime utcNow)
    {
        TourOffer? best = null;
        foreach (var offer in offers)
        {
            if (!IsFutureDeparture(offer.ValidFrom, utcNow))
                continue;

            if (best is null
                || offer.PricePerPerson < best.PricePerPerson
                || (offer.PricePerPerson == best.PricePerPerson && offer.ValidFrom < best.ValidFrom))
            {
                best = offer;
            }
        }

        return best?.Currency;
    }

    public static DateTime? NearestDeparture(IEnumerable<TourOffer> offers, DateTime utcNow)
    {
        DateTime? nearest = null;
        foreach (var offer in offers)
        {
            if (!IsFutureDeparture(offer.ValidFrom, utcNow))
                continue;

            if (nearest is null || offer.ValidFrom < nearest)
                nearest = offer.ValidFrom;
        }

        return nearest;
    }

    public static IReadOnlyList<TourOffer> FutureOffers(IEnumerable<TourOffer> offers, DateTime utcNow) =>
        offers.Where(offer => IsFutureDeparture(offer.ValidFrom, utcNow))
            .OrderBy(offer => offer.ValidFrom)
            .ToList();
}
