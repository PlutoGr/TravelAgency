namespace TravelAgency.Booking.Application.Exceptions;

public sealed class BadRequestException(string message) : AppException(message, 400);
