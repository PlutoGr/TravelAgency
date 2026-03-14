namespace TravelAgency.Catalog.Application.DTOs;

public record DirectionDto(
    Guid Id,
    string Name,
    string Country,
    string? Description);
