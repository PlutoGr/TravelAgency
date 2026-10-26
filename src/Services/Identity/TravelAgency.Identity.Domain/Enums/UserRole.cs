namespace TravelAgency.Identity.Domain.Enums;

public enum UserRole
{
    Client,
    Manager,
    Admin
}

public static class UserRoleExtensions
{
    public static string ToRoleString(this UserRole role) => role switch
    {
        UserRole.Client => "Client",
        UserRole.Manager => "Manager",
        UserRole.Admin => "Admin",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    public static UserRole FromString(string role) => role switch
    {
        "Client" => UserRole.Client,
        "Manager" => UserRole.Manager,
        "Admin" => UserRole.Admin,
        _ => throw new ArgumentException($"Unknown role: {role}", nameof(role))
    };

    public static int Rank(this UserRole role) => role switch
    {
        UserRole.Client => 0,
        UserRole.Manager => 1,
        UserRole.Admin => 2,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
    };

    public static bool IsDowngradeTo(this UserRole current, UserRole next) => next.Rank() < current.Rank();
}
