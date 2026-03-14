namespace TravelAgency.Booking.Domain.Exceptions;

public class BookingDomainException : Exception
{
    public BookingDomainException(string message) : base(message) { }

    public BookingDomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
