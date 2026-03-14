namespace TravelAgency.Booking.Application.DTOs.Requests;

public record CreateBookingRequest(Guid TourId, string? Comment);
