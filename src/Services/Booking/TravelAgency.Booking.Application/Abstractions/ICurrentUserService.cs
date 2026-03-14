namespace TravelAgency.Booking.Application.Abstractions;

public interface ICurrentUserService
{
    Guid UserId { get; }
    string Role { get; }
}
