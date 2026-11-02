using Microsoft.EntityFrameworkCore;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Booking.Infrastructure.Persistence;

namespace TravelAgency.Booking.Infrastructure.Repositories;

public class ClientNoteRepository : IClientNoteRepository
{
    private readonly BookingDbContext _context;

    public ClientNoteRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClientNote>> ListByClientAsync(Guid clientId, CancellationToken ct = default)
    {
        return await _context.ClientNotes
            .Where(note => note.ClientId == clientId)
            .ToListAsync(ct);
    }

    public void Stage(ClientNote note)
    {
        _context.ClientNotes.Add(note);
    }
}
