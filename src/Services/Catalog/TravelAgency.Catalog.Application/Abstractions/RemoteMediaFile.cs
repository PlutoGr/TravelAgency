namespace TravelAgency.Catalog.Application.Abstractions;

public static class RemoteMediaPurposes
{
    public const string TourImage = "tour-image";
}

public sealed record RemoteMediaFile(
    Guid Id,
    string OwnerId,
    int Width,
    int Height,
    string Purpose = RemoteMediaPurposes.TourImage);
