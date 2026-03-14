using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Domain.Interfaces;

public interface IOutboxMessageRepository
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(int batchSize, CancellationToken ct = default);
    void Stage(OutboxMessage message);
}
