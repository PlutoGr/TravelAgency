using BookingEntity = TravelAgency.Booking.Domain.Entities.Booking;

namespace TravelAgency.Booking.Domain.Interfaces;

public interface IBookingRepository
{
    Task<BookingEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<BookingEntity>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);
    Task<IReadOnlyList<BookingEntity>> GetAllForManagersAsync(CancellationToken ct = default);

    /// <summary>
    /// Ответственный по последней заявке клиента, у которой он назначен.
    /// Учитываются отменённые и завершённые заявки.
    /// </summary>
    Task<Guid?> GetPreferredManagerIdAsync(Guid clientId, CancellationToken ct = default);

    void Stage(BookingEntity booking);
}
