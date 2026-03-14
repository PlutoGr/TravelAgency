using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

public class TourPrice
{
    public Guid Id { get; private set; }
    public Guid TourId { get; private set; }
    public DateTime ValidFrom { get; private set; }
    public DateTime ValidTo { get; private set; }
    public decimal PricePerPerson { get; private set; }
    public string Currency { get; private set; } = "USD";
    public int AvailableSeats { get; private set; }

    private TourPrice() { }

    public static TourPrice Create(
        Guid tourId,
        DateTime validFrom,
        DateTime validTo,
        decimal pricePerPerson,
        string currency,
        int availableSeats)
    {
        if (pricePerPerson <= 0)
            throw new CatalogDomainException("Price per person must be greater than zero.");

        if (availableSeats < 0)
            throw new CatalogDomainException("Available seats cannot be negative.");

        if (validFrom >= validTo)
            throw new CatalogDomainException("ValidFrom must be earlier than ValidTo.");

        return new TourPrice
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            ValidFrom = validFrom,
            ValidTo = validTo,
            PricePerPerson = pricePerPerson,
            Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant(),
            AvailableSeats = availableSeats
        };
    }
}
