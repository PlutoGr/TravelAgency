namespace TravelAgency.Identity.Application.Settings;

public sealed class MediaClientSettings
{
    public const string SectionName = "MediaClient";

    /// <summary>Base URL of Media HTTP API, e.g. http://media-service:8080</summary>
    public string BaseUrl { get; set; } = "http://media-service:8080";
}
