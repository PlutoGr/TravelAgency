using TravelAgency.Catalog.Domain.Enums;

namespace TravelAgency.Catalog.Application.DTOs;

public record UpdateTourRequest(
    string Title,
    string Description,
    TourType TourType,
    string Country,
    int DurationDays,
    string? ImageUrl,
    Guid? DirectionId);
