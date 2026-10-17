namespace TravelAgency.Identity.Application.DTOs;

/// <summary>
/// Пара токенов. ExpiresAt относится к access-токену, RefreshExpiresAt к refresh-токену.
/// </summary>
public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    DateTime RefreshExpiresAt);
