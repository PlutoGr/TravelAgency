namespace TravelAgency.Booking.Domain.Entities;

public class ClientNote
{
    public const int MaxTextLength = 500;

    public Guid Id { get; private set; }
    public Guid ClientId { get; private set; }
    public Guid AuthorId { get; private set; }
    public string Text { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private ClientNote() { }

    public static ClientNote Create(Guid clientId, Guid authorId, string text, DateTime? createdAt = null)
    {
        if (clientId == Guid.Empty)
            throw new Exceptions.BookingDomainException("ClientId must not be empty.");
        if (authorId == Guid.Empty)
            throw new Exceptions.BookingDomainException("AuthorId must not be empty.");
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaxTextLength)
            throw new Exceptions.BookingDomainException("Note text is not valid.");

        return new ClientNote
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            AuthorId = authorId,
            Text = text,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }
}
