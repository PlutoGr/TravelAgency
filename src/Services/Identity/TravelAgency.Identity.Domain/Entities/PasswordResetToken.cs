namespace TravelAgency.Identity.Domain.Entities;

/// <summary>
/// Одноразовая ссылка восстановления. В таблице только хеш секрета.
/// </summary>
public class PasswordResetToken
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string TokenHash { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Create(Guid userId, string secret, DateTime expiresAt)
    {
        return new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = RefreshToken.Hash(secret),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }
}
