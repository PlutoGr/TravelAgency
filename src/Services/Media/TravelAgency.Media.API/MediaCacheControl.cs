namespace TravelAgency.Media.API;

public static class MediaCacheControl
{
    public const string PublicImmutable = "public, max-age=31536000, immutable";
    public const string PrivateNoStore = "private, no-store";
    public const string NoStore = "no-store";
}
