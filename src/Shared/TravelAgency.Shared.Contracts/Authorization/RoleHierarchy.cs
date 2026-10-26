namespace TravelAgency.Shared.Contracts.Authorization;

/// <summary>
/// Иерархия ролей: client, затем manager, затем admin.
/// Проверка «не ниже» заменяет отдельные списки ролей.
/// </summary>
public static class RoleHierarchy
{
    public static int Rank(string? role) => role switch
    {
        AppRoles.Client => 1,
        AppRoles.Manager => 2,
        AppRoles.Admin => 3,
        _ => 0,
    };

    public static bool IsAtLeast(string? role, string minimum)
    {
        var required = Rank(minimum);
        return required > 0 && Rank(role) >= required;
    }

    public static string[] RolesAtLeast(string minimum) => minimum switch
    {
        AppRoles.Client => [AppRoles.Client, AppRoles.Manager, AppRoles.Admin],
        AppRoles.Manager => [AppRoles.Manager, AppRoles.Admin],
        AppRoles.Admin => [AppRoles.Admin],
        _ => throw new ArgumentOutOfRangeException(nameof(minimum), minimum, "Unknown role."),
    };
}
