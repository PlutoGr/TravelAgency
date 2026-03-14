using TravelAgency.Booking.Domain.Enums;

namespace TravelAgency.Booking.Application.DTOs.Requests;

public record ChangeBookingStatusRequest(BookingStatus NewStatus);
