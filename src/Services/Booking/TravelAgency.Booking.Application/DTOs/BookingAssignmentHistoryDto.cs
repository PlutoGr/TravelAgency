namespace TravelAgency.Booking.Application.DTOs;

public record BookingAssignmentHistoryDto(
    Guid Id,
    Guid? PreviousManagerId,
    Guid NewManagerId,
    Guid ChangedByUserId,
    DateTime ChangedAt,
    string? Comment);
