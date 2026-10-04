using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

/// <summary>
/// Задел этапа 2 для составного тура. На этапе 1 строки не создаются.
/// </summary>
public class TourComponent
{
    public Guid Id { get; private set; }
    public Guid TourId { get; private set; }
    public string Name { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }

    private TourComponent() { }

    public static TourComponent Create(Guid tourId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new CatalogDomainException("Component name cannot be empty.");

        return new TourComponent
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
