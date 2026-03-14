using TravelAgency.Chat.Application.DTOs;
using TravelAgency.Chat.Domain.Entities;

namespace TravelAgency.Chat.Application.Mapping;

public static class ChatMessageMapper
{
    /// <summary>
    /// Maps a domain ChatMessage to ChatMessageDto.
    /// </summary>
    public static ChatMessageDto ToDto(this ChatMessage message) =>
        new(
            message.Id,
            message.BookingId,
            message.SenderId,
            message.SenderName,
            message.SenderRole.ToString(),
            message.Text,
            message.Attachments,
            message.CreatedAt);
}
