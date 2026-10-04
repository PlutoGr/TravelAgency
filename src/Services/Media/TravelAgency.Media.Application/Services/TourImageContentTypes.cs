namespace TravelAgency.Media.Application.Services;

public static class TourImageContentTypes
{
    public static readonly string[] Allowed = ["image/jpeg", "image/png", "image/webp"];

    public static bool IsAllowed(string? contentType) =>
        contentType is not null && Allowed.Contains(contentType, StringComparer.OrdinalIgnoreCase);
}
