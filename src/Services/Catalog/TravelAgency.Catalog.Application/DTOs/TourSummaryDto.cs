using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.DTOs;

public record TourSummaryDto(
    Guid Id,
    string Title,
    string Country,
    TourType TourType,
    int DurationDays,
    string? ImageUrl,
    decimal? MinPrice,
    string? Currency,
    bool IsActive);
