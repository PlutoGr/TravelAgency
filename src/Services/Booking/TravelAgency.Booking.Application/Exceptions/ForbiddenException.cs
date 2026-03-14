namespace TravelAgency.Booking.Application.Exceptions;

public sealed class ForbiddenException(string message) : AppException(message, 403);
