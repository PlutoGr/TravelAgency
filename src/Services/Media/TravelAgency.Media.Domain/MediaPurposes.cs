namespace TravelAgency.Media.Domain;

/// <summary>
/// Why a file was uploaded. Public tour URLs only serve <see cref="TourImage"/>.
/// Public avatar URLs serve <see cref="Avatar"/>.
/// </summary>
public static class MediaPurposes
{
    public const string General = "general";
    public const string TourImage = "tour-image";
    public const string Avatar = "avatar";

    public static bool IsTourImage(string? purpose) =>
        string.Equals(purpose, TourImage, StringComparison.OrdinalIgnoreCase);

    public static bool IsAvatar(string? purpose) =>
        string.Equals(purpose, Avatar, StringComparison.OrdinalIgnoreCase);
}
