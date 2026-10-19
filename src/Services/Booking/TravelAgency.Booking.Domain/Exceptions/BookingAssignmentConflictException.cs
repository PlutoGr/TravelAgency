namespace TravelAgency.Booking.Domain.Exceptions;

public sealed class BookingAssignmentConflictException : Exception
{
    public const string DefaultMessage = "Заявку уже взял другой менеджер";

    public BookingAssignmentConflictException() : base(DefaultMessage) { }
}
