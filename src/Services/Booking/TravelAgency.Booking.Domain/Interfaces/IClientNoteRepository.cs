using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Domain.Interfaces;

public interface IClientNoteRepository
{
    Task<IReadOnlyList<ClientNote>> ListByClientAsync(Guid clientId, CancellationToken ct = default);

    void Stage(ClientNote note);
}
