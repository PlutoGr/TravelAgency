namespace TravelAgency.Catalog.Application.DTOs;

public record CatalogTourSummaryDto(
    Guid TourId,
    string Title,
    string Country,
    int DurationDays,
    decimal? PricePerPerson,
    string? Currency);
