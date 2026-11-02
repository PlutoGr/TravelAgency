using System.Text.Json.Serialization;
using TravelAgency.Booking.Domain.Enums;

namespace TravelAgency.Booking.Application.DTOs;

public sealed record ClientCardBookingDto(
    Guid Id,
    Guid TourId,
    BookingStatus Status,
    DateTime CreatedAt);

public sealed record ClientNoteDto(
    Guid Id,
    string Text,
    string AuthorName,
    DateTime CreatedAt);

public sealed record ClientCardDto(
    Guid Id,
    string DisplayName,
    string Email,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? AvatarUrl,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Phone,
    IReadOnlyList<ClientCardBookingDto> Bookings,
    IReadOnlyList<ClientNoteDto> Notes);

public sealed record ClientListItemDto(
    Guid Id,
    string DisplayName,
    string Email,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? AvatarUrl,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    string? Phone,
    int BookingCount);
