using TravelAgency.Booking.Domain.Entities;

namespace TravelAgency.Booking.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task<Favorite?> GetAsync(Guid userId, Guid tourId, CancellationToken ct = default);
    Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    void Stage(Favorite favorite);
    void Remove(Favorite favorite);
}
