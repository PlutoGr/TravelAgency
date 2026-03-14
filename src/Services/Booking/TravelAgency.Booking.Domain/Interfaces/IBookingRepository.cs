using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.Domain.Interfaces;

public interface IBookingRepository
{
    Task<BookingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BookingEntity>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);
    void Stage(BookingEntity booking);
}
