namespace TravelAgency.Booking.Application.DTOs;

public record UserSummaryDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string Role);
