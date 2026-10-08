namespace TravelAgency.Media.Application.Services;

/// <summary>
/// Widths of legacy image thumbnails. Each width is stored at most once.
/// </summary>
public static class ThumbnailWidthList
{
    public static readonly int[] Default = [200, 800];

    public static int[] DistinctPositive(IEnumerable<int>? widths) =>
        (widths ?? [])
            .Where(width => width > 0)
            .Distinct()
            .ToArray();
}
