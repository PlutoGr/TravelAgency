namespace TravelAgency.Media.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<string> GeneratePresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default);
}
