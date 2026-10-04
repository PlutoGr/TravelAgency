namespace TravelAgency.Booking.Application.Exceptions;

public sealed class TourUnavailableException() : AppException("Tour is unavailable.", 422)
{
    public const string Code = "tour-unavailable";
}
