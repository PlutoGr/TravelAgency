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
    bool IsActive,
    decimal? PriceFrom = null,
    DateTime? NearestDate = null,
    string? ShortDescription = null,
    string? DepartureCity = null,
    IReadOnlyList<PublicTourPreviewDto>? Previews = null);
