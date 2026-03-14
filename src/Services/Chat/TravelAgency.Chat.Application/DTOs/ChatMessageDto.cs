namespace TravelAgency.Chat.Application.DTOs;

/// <summary>
/// Data transfer object for a chat message.
/// </summary>
public record ChatMessageDto(
    Guid Id,
    Guid BookingId,
    string SenderId,
    string SenderName,
    string SenderRole,
    string Text,
    IReadOnlyList<string>? Attachments,
    DateTime CreatedAt);
