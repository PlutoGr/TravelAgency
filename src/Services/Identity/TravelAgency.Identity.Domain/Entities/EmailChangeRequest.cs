namespace TravelAgency.Identity.Domain.Entities;

/// <summary>
/// Пара ссылок подтверждения и отмены смены почты. Неподтверждённый адрес живёт только здесь.
/// </summary>
public class EmailChangeRequest
{
    public const int MaxEmailLength = 256;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string NewEmail { get; private set; } = null!;
    public string ConfirmTokenHash { get; private set; } = null!;
    public string CancelTokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private EmailChangeRequest() { }

    public static EmailChangeRequest Create(
        Guid userId,
        string newEmail,
        string confirmSecret,
        string cancelSecret,
        DateTime expiresAt)
    {
        return new EmailChangeRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            NewEmail = newEmail,
            ConfirmTokenHash = RefreshToken.Hash(confirmSecret),
            CancelTokenHash = RefreshToken.Hash(cancelSecret),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }
}
