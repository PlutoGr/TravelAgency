namespace TravelAgency.Shared.Contracts.Abstractions;

/// <summary>
/// Provides the current authenticated user's identity from JWT claims.
/// Implementations read from HttpContext.User. Role values use <see cref="Authorization.AppRoles"/> constants.
/// </summary>
public interface ICurrentUserService
{
    Guid UserId { get; }
    string Role { get; }
    bool IsAuthenticated { get; }
    /// <summary>
    /// Email from claims when available (e.g. Identity service). Empty string otherwise.
    /// </summary>
    string Email { get; }
    /// <summary>
    /// Display name from claims (e.g. "name", "preferred_username") or "User {UserId}" if not available.
    /// </summary>
    string DisplayName { get; }
}
