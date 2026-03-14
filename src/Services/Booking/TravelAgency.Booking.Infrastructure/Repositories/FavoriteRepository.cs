using Microsoft.EntityFrameworkCore;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Infrastructure.Persistence;

namespace TravelAgency.Booking.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly BookingDbContext _context;

    public FavoriteRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Favorite?> GetAsync(Guid userId, Guid tourId, CancellationToken ct = default)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(f => f.UserId == userId && f.TourId == tourId, ct);
    }

    public async Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _context.Favorites
            .Where(f => f.UserId == userId)
            .ToListAsync(ct);
    }

    public void Stage(Favorite favorite)
    {
        _context.Favorites.Add(favorite);
    }

    public void Remove(Favorite favorite)
    {
        _context.Favorites.Remove(favorite);
    }
}
