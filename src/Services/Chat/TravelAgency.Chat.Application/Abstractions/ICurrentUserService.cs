namespace TravelAgency.Chat.Application.Abstractions;

/// <summary>
/// Provides the current authenticated user's identity and role from JWT claims.
/// Role values use <see cref="TravelAgency.Shared.Contracts.Authorization.AppRoles"/> constants.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Role { get; }
    /// <summary>
    /// Display name from claims (e.g. "name", "preferred_username") or "User {UserId}" if not available.
    /// </summary>
    string DisplayName { get; }
}
