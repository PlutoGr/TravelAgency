namespace TravelAgency.Booking.Application.DTOs;

public record FavoriteDto(
    Guid Id,
    Guid UserId,
    Guid TourId,
    DateTime AddedAt);
