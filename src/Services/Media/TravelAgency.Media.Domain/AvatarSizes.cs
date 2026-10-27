namespace TravelAgency.Media.Domain;

/// <summary>Fixed square preview for user avatars.</summary>
public static class AvatarSizes
{
    public const string A256 = "a256";
    public const int SidePixels = 256;
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public static bool IsKnown(string? size) =>
        string.Equals(size, A256, StringComparison.Ordinal);
}
