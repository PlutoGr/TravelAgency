namespace TravelAgency.Media.Domain;

/// <summary>
/// Preview slots for a tour image. The code is a max width, not a promise to upscale.
/// </summary>
public static class TourImageSizes
{
    public const string W200 = "w200";
    public const string W800 = "w800";
    public const string W1600 = "w1600";

    public static readonly IReadOnlyList<(string Code, int MaxWidth)> All =
    [
        (W200, 200),
        (W800, 800),
        (W1600, 1600)
    ];

    public static bool IsKnown(string? size) =>
        All.Any(s => string.Equals(s.Code, size, StringComparison.Ordinal));
}
