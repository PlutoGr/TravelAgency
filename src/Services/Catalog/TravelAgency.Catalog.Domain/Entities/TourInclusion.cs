using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

public class TourInclusion
{
    public Guid Id { get; private set; }
    public Guid TourId { get; private set; }
    public string Text { get; private set; } = default!;
    public TourInclusionKind Kind { get; private set; }
    public int SortOrder { get; private set; }

    /// <summary>Задел этапа 2: пункт может относиться к компоненту составного тура.</summary>
    public Guid? ComponentId { get; private set; }

    private TourInclusion() { }

    public static TourInclusion Create(
        Guid tourId,
        string text,
        TourInclusionKind kind,
        int sortOrder = 0,
        Guid? componentId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new CatalogDomainException("Inclusion text cannot be empty.");

        if (sortOrder < 0)
            throw new CatalogDomainException("Inclusion sort order cannot be negative.");

        return new TourInclusion
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            Text = text.Trim(),
            Kind = kind,
            SortOrder = sortOrder,
            ComponentId = componentId
        };
    }
}
