namespace TravelAgency.Media.Application.Services;

/// <summary>
/// Fits an image into a max width without upscaling and without changing the aspect ratio.
/// </summary>
public static class PreviewSizer
{
    public static (int Width, int Height) FitWithin(int originalWidth, int originalHeight, int maxWidth)
    {
        if (originalWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(originalWidth), "Width must be positive.");
        if (originalHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(originalHeight), "Height must be positive.");
        if (maxWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxWidth), "Max width must be positive.");

        if (originalWidth <= maxWidth)
            return (originalWidth, originalHeight);

        var height = (int)Math.Round(
            originalHeight * (maxWidth / (double)originalWidth),
            MidpointRounding.AwayFromZero);
        if (height < 1)
            height = 1;

        return (maxWidth, height);
    }
}
