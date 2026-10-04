namespace TravelAgency.Media.Domain;

/// <summary>
/// Why a file was uploaded. Public tour URLs only serve <see cref="TourImage"/>.
/// </summary>
public static class MediaPurposes
{
    public const string General = "general";
    public const string TourImage = "tour-image";

    public static bool IsTourImage(string? purpose) =>
        string.Equals(purpose, TourImage, StringComparison.OrdinalIgnoreCase);
}
