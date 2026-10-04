namespace TravelAgency.Catalog.Domain;

/// <summary>
/// Утверждённые лимиты этапа 1. Ширина обложки проверяется, когда значение уже известно.
/// </summary>
public static class TourContentLimits
{
    public const int ShortDescriptionMaxLength = 300;
    public const int MaxImagesPerTour = 20;
    public const int MinImagesToPublish = 3;
    public const int CoverMinWidthPx = 1280;
}
