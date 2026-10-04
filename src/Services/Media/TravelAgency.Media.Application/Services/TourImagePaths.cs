namespace TravelAgency.Media.Application.Services;

/// <summary>
/// Browser-facing paths through the Gateway. Draft photos use the manage path
/// because the public path answers 404 until the file is published.
/// </summary>
public static class TourImagePaths
{
    public static string Manage(Guid id, string size) =>
        $"/api/v1/media/manage/files/{id:D}/{size}";

    public static string Public(Guid id, string size) =>
        $"/api/v1/media/files/{id:D}/{size}";
}
