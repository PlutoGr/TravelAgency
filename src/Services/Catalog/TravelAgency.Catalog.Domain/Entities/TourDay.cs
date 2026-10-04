using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

public class TourDay
{
    public Guid Id { get; private set; }
    public Guid TourId { get; private set; }
    public int DayNumber { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;

    /// <summary>Задел этапа 2: день может относиться к компоненту составного тура.</summary>
    public Guid? ComponentId { get; private set; }

    private TourDay() { }

    public static TourDay Create(
        Guid tourId,
        int dayNumber,
        string title,
        string description,
        Guid? componentId = null)
    {
        if (dayNumber < 1)
            throw new CatalogDomainException("Day number must be at least 1.");

        if (string.IsNullOrWhiteSpace(title))
            throw new CatalogDomainException("Day title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new CatalogDomainException("Day description cannot be empty.");

        return new TourDay
        {
            Id = Guid.NewGuid(),
            TourId = tourId,
            DayNumber = dayNumber,
            Title = title.Trim(),
            Description = description.Trim(),
            ComponentId = componentId
        };
    }
}
