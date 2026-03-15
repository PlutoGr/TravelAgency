namespace TravelAgency.Shared.Contracts.Authorization;

public static class AuthPolicies
{
    public const string RequireAuthenticated = nameof(RequireAuthenticated);
    public const string RequireManager = nameof(RequireManager);
    public const string RequireAdmin = nameof(RequireAdmin);
    public const string RequireManagerOrAdmin = nameof(RequireManagerOrAdmin);
}
