namespace TravelAgency.Booking.Application.DTOs;

public record BookingTourSnapshotDto(
    Guid TourId,
    string Title,
    string Description,
    decimal Price,
    string Currency,
    int DurationDays,
    DateTime SnapshotTakenAt);
