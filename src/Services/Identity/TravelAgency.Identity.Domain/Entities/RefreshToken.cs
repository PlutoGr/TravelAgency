using System.Security.Cryptography;
using System.Text;
using TravelAgency.Identity.Domain.Exceptions;

namespace TravelAgency.Identity.Domain.Entities;

public class RefreshToken
{
    public const int HashLength = 64;

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    /// <summary>SHA-256 (hex) от значения токена. Само значение не хранится.</summary>
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    /// <summary>SHA-256 (hex) токена, который заменил этот, либо null.</summary>
    public string? ReplacedByToken { get; private set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;

    private RefreshToken() { }

    public static string Hash(string token)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        return new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = Hash(token),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Revoke(string? replacedByToken = null)
    {
        if (IsRevoked)
            throw new IdentityDomainException("Refresh token has already been revoked.");

        RevokedAt = DateTime.UtcNow;
        ReplacedByToken = replacedByToken is null ? null : Hash(replacedByToken);
    }
}
