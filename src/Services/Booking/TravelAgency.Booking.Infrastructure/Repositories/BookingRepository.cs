using Microsoft.EntityFrameworkCore;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Infrastructure.Persistence;

namespace TravelAgency.Booking.Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly BookingDbContext _context;

    public BookingRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.Proposals)
            .Include(b => b.StatusHistory)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<IReadOnlyList<Domain.Entities.Booking>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.Proposals)
            .Where(b => b.ClientId == clientId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public void Stage(Domain.Entities.Booking booking)
    {
        _context.Bookings.Add(booking);
    }
}
