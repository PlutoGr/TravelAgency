using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.DTOs;

public enum TourSortBy { CreatedAt, Title, Price, DurationDays }
public enum SortDirection { Asc, Desc }

public record ToursFilterDto(
    string? Country = null,
    Guid? DirectionId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    TourType? TourType = null,
    bool? IsActive = true,
    TourSortBy SortBy = TourSortBy.CreatedAt,
    SortDirection SortDirection = SortDirection.Desc,
    int Page = 1,
    int PageSize = 20);
