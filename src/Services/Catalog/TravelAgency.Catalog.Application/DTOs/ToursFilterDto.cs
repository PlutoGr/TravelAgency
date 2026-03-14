using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.DTOs;

public record ToursFilterDto(
    string? Country = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    TourType? TourType = null,
    bool? IsActive = true,
    int Page = 1,
    int PageSize = 20);
