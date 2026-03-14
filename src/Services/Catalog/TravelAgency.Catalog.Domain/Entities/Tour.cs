using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

public class Tour
{
    private readonly List<TourPrice> _prices = [];

    public Guid Id { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public TourType TourType { get; private set; }
    public Guid? DirectionId { get; private set; }
    public string Country { get; private set; } = default!;
    public int DurationDays { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<TourPrice> Prices => _prices.AsReadOnly();

    private Tour() { }

    public static Tour Create(
        string title,
        string description,
        TourType tourType,
        string country,
        int durationDays,
        string? imageUrl,
        Guid? directionId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new CatalogDomainException("Tour title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new CatalogDomainException("Tour description cannot be empty.");

        if (string.IsNullOrWhiteSpace(country))
            throw new CatalogDomainException("Tour country cannot be empty.");

        if (durationDays < 1)
            throw new CatalogDomainException("Tour duration must be at least 1 day.");

        return new Tour
        {
            Id = Guid.NewGuid(),
            Title = title.Trim(),
            Description = description.Trim(),
            TourType = tourType,
            DirectionId = directionId,
            Country = country.Trim(),
            DurationDays = durationDays,
            ImageUrl = imageUrl?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string description,
        TourType tourType,
        string country,
        int durationDays,
        string? imageUrl,
        Guid? directionId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new CatalogDomainException("Tour title cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new CatalogDomainException("Tour description cannot be empty.");

        if (string.IsNullOrWhiteSpace(country))
            throw new CatalogDomainException("Tour country cannot be empty.");

        if (durationDays < 1)
            throw new CatalogDomainException("Tour duration must be at least 1 day.");

        Title = title.Trim();
        Description = description.Trim();
        TourType = tourType;
        DirectionId = directionId;
        Country = country.Trim();
        DurationDays = durationDays;
        ImageUrl = imageUrl?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPrices(IEnumerable<TourPrice> prices)
    {
        _prices.Clear();
        _prices.AddRange(prices);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }
}
