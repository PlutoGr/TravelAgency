using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Settings;

namespace TravelAgency.Media.Infrastructure.Storage;

public sealed class S3StorageService(
    IAmazonS3 s3Client,
    IOptions<StorageSettings> options
) : IStorageService
{
    private readonly StorageSettings _settings = options.Value;

    public async Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken ct = default)
    {
        var request = new PutObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            DisablePayloadSigning = true,
            AutoCloseStream = false
        };

        await s3Client.PutObjectAsync(request, ct);
        return key;
    }

    public async Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        var request = new GetObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key
        };

        var response = await s3Client.GetObjectAsync(request, ct);
        var ms = new MemoryStream();
        await response.ResponseStream.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _settings.BucketName,
            Key = key
        };

        await s3Client.DeleteObjectAsync(request, ct);
    }

    public Task<string> GeneratePresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _settings.BucketName,
            Key = key,
            Expires = DateTime.UtcNow.Add(ttl),
            Protocol = Protocol.HTTP
        };

        var url = s3Client.GetPreSignedURL(request);
        return Task.FromResult(url);
    }
}
