using System.Security.Cryptography;

namespace TravelAgency.Identity.Domain.Entities;

/// <summary>
/// Секрет ссылки: 32 случайных байта, base64url без padding. В базе только SHA-256, как у refresh.
/// </summary>
public static class LinkSecret
{
    public const int ByteLength = 32;

    public static string Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(ByteLength);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string secret) => RefreshToken.Hash(secret);
}
