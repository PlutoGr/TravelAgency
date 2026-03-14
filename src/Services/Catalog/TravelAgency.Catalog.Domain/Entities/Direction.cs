using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.Domain.Entities;

public class Direction
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Country { get; private set; } = default!;
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Direction() { }

    public static Direction Create(string name, string country, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new CatalogDomainException("Direction name cannot be empty.");

        if (string.IsNullOrWhiteSpace(country))
            throw new CatalogDomainException("Direction country cannot be empty.");

        return new Direction
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Country = country.Trim(),
            Description = description?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string country, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new CatalogDomainException("Direction name cannot be empty.");

        if (string.IsNullOrWhiteSpace(country))
            throw new CatalogDomainException("Direction country cannot be empty.");

        Name = name.Trim();
        Country = country.Trim();
        Description = description?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
