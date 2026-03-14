namespace TravelAgency.Media.Application.Settings;

public sealed class StorageSettings
{
    public string ServiceUrl { get; init; } = string.Empty;
    public string AccessKey { get; init; } = string.Empty;
    public string SecretKey { get; init; } = string.Empty;
    public string BucketName { get; init; } = string.Empty;
    public bool ForcePathStyle { get; init; } = true;
    public int PresignTtlMinutes { get; init; } = 60;
}
