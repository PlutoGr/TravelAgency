using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.DTOs;

public record TourDto(
    Guid Id,
    string Title,
    string Description,
    string Country,
    TourType TourType,
    int DurationDays,
    string? ImageUrl,
    Guid? DirectionId,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<TourPriceDto> Prices);
