namespace TravelAgency.Catalog.Application.DTOs;

public record UpdateTourPricesRequest(List<TourPriceRequest> Prices);

public record TourPriceRequest(
    DateTime ValidFrom,
    DateTime ValidTo,
    decimal PricePerPerson,
    string Currency,
    int AvailableSeats);
