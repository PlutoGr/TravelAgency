using TravelAgency.Chat.Domain.Entities;

namespace TravelAgency.Chat.Domain.Interfaces;

public interface IChatMessageRepository
{
    Task<IReadOnlyList<ChatMessage>> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<ChatMessage> AddAsync(ChatMessage message, CancellationToken ct = default);
}
