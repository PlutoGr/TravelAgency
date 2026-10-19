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
            .Include(b => b.AssignmentHistory)
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

    public async Task<IReadOnlyList<Domain.Entities.Booking>> GetAllForManagersAsync(CancellationToken ct = default)
    {
        return await _context.Bookings
            .Include(b => b.Proposals)
            .Include(b => b.AssignmentHistory)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Guid?> GetPreferredManagerIdAsync(Guid clientId, CancellationToken ct = default)
    {
        return await _context.Bookings
            .Where(b => b.ClientId == clientId && b.AssignedManagerId != null)
            .OrderByDescending(b => b.CreatedAt)
            .ThenByDescending(b => b.AssignedAt)
            .Select(b => b.AssignedManagerId)
            .FirstOrDefaultAsync(ct);
    }

    public void Stage(Domain.Entities.Booking booking)
    {
        _context.Bookings.Add(booking);
    }
}
