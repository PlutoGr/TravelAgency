using TravelAgency.Booking.Domain.Enums;

namespace TravelAgency.Booking.Application.DTOs;

public record BookingDto(
    Guid Id,
    Guid ClientId,
    Guid TourId,
    string? Comment,
    BookingStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<ProposalDto> Proposals,
    string? ClientName = null,
    string? ClientEmail = null,
    string? ClientPhone = null);
