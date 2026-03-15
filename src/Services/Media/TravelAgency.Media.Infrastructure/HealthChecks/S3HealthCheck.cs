using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Settings;

namespace TravelAgency.Media.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies S3/MinIO connectivity by probing the configured bucket.
/// </summary>
public sealed class S3HealthCheck(
    IAmazonS3 s3Client,
    IOptions<StorageSettings> options
) : IHealthCheck
{
    private readonly StorageSettings _settings = options.Value;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await s3Client.GetBucketLocationAsync(_settings.BucketName, cancellationToken);
            return HealthCheckResult.Healthy("S3 bucket is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("S3 bucket is unreachable.", ex);
        }
    }
}
