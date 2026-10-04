namespace TravelAgency.Catalog.Domain.Exceptions;

/// <summary>
/// Тур нельзя опубликовать или опубликованный тур нельзя оставить в состоянии,
/// которое не проходит проверку. Статус при этом не меняется.
/// </summary>
public sealed class TourNotPublishableException : CatalogDomainException
{
    public IReadOnlyList<string> Missing { get; }

    public TourNotPublishableException(IReadOnlyList<string> missing)
        : base("Tour is not publishable. Missing: " + string.Join(", ", missing))
    {
        Missing = missing;
    }
}
