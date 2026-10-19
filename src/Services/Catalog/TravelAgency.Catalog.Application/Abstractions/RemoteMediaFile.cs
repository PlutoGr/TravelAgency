namespace TravelAgency.Catalog.Application.Abstractions;

public static class RemoteMediaPurposes
{
    public const string TourImage = "tour-image";
}

/// <summary>
/// Файл из Media. Purpose обязателен: без него нельзя молча считать файл фото тура.
/// </summary>
public sealed record RemoteMediaFile(
    Guid Id,
    string OwnerId,
    int Width,
    int Height,
    string Purpose);
