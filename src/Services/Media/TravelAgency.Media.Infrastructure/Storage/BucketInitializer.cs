using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Settings;

namespace TravelAgency.Media.Infrastructure.Storage;

/// <summary>
/// Ensures the configured S3/MinIO bucket exists on application startup.
/// Creates the bucket if it does not exist so that the S3 health check can pass.
/// </summary>
public sealed class BucketInitializer(
    IAmazonS3 s3Client,
    IOptions<StorageSettings> options,
    ILogger<BucketInitializer> logger
) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var bucketName = options.Value.BucketName;
        if (string.IsNullOrWhiteSpace(bucketName))
        {
            logger.LogWarning("Storage:BucketName is not configured; skipping bucket initialization.");
            return;
        }

        try
        {
            var exists = await BucketExistsAsync(bucketName, cancellationToken);
            if (exists)
            {
                logger.LogDebug("S3 bucket {BucketName} already exists.", bucketName);
                return;
            }

            await s3Client.PutBucketAsync(new PutBucketRequest
            {
                BucketName = bucketName,
                UseClientRegion = true
            }, cancellationToken);
            logger.LogInformation("Created S3 bucket {BucketName}.", bucketName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure S3 bucket {BucketName} exists. Health check may fail.", bucketName);
            // Do not throw - allow app to start; health check will report unhealthy
        }
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private async Task<bool> BucketExistsAsync(string bucketName, CancellationToken cancellationToken)
    {
        var response = await s3Client.ListBucketsAsync(cancellationToken);
        return response.Buckets?.Any(b => string.Equals(b.BucketName, bucketName, StringComparison.Ordinal)) ?? false;
    }
}
