using Microsoft.EntityFrameworkCore;
using TravelAgency.Chat.Domain.Entities;
using TravelAgency.Chat.Domain.Interfaces;
using TravelAgency.Chat.Infrastructure.Persistence;

namespace TravelAgency.Chat.Infrastructure.Repositories;

public class MessageRepository : IChatMessageRepository
{
    private readonly ChatDbContext _db;

    public MessageRepository(ChatDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.Messages
            .Where(m => m.BookingId == bookingId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken ct = default)
    {
        _db.Messages.Add(message);
        await _db.SaveChangesAsync(ct);
        return message;
    }
}
