namespace TravelAgency.Catalog.Application.DTOs;

public record TourPriceDto(
    Guid Id,
    DateTime ValidFrom,
    DateTime ValidTo,
    decimal PricePerPerson,
    string Currency,
    int AvailableSeats);
