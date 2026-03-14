using TravelAgency.Chat.Domain.Enums;
using TravelAgency.Chat.Domain.Exceptions;

namespace TravelAgency.Chat.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; private set; }
    public Guid BookingId { get; private set; }
    /// <summary>Stored as string (max 100) in DB per MessageEntityConfiguration.</summary>
    public string SenderId { get; private set; } = string.Empty;
    public string SenderName { get; private set; } = string.Empty;
    public SenderRole SenderRole { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public IReadOnlyList<string>? Attachments { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private ChatMessage() { }

    public static ChatMessage Create(
        Guid bookingId,
        string senderId,
        string senderName,
        SenderRole senderRole,
        string text,
        IReadOnlyList<string>? attachments = null)
    {
        if (bookingId == Guid.Empty)
            throw new ChatDomainException("BookingId must not be empty.");

        if (string.IsNullOrWhiteSpace(senderId))
            throw new ChatDomainException("SenderId must not be null or empty.");

        if (senderId.Length > 100)
            throw new ChatDomainException("SenderId must not exceed 100 characters.");

        if (string.IsNullOrWhiteSpace(senderName))
            throw new ChatDomainException("SenderName must not be null or empty.");

        if (senderName.Length > 200)
            throw new ChatDomainException("SenderName must not exceed 200 characters.");

        if (!Enum.IsDefined(senderRole))
            throw new ChatDomainException($"Invalid SenderRole: {senderRole}.");

        if (text is null)
            throw new ChatDomainException("Text must not be null.");

        if (string.IsNullOrWhiteSpace(text) && (attachments is null || attachments.Count == 0))
            throw new ChatDomainException("Text or attachments must be provided.");

        return new ChatMessage
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            SenderId = senderId,
            SenderName = senderName,
            SenderRole = senderRole,
            Text = text,
            Attachments = attachments?.Count > 0 ? attachments.ToList().AsReadOnly() : null,
            CreatedAt = DateTime.UtcNow
        };
    }
}
