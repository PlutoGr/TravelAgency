using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Media.Application;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Application.Services;
using TravelAgency.Media.Infrastructure.HealthChecks;
using TravelAgency.Media.Infrastructure.Maintenance;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Media.Infrastructure.Repositories;
using TravelAgency.Media.Infrastructure.Services;
using TravelAgency.Media.Infrastructure.Storage;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Media.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMediaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediaApplication();
        services.AddSharedMediatRBehaviors();

        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.AddUploadSettings(configuration);

        var connectionString = configuration.GetConnectionString("MediaDb");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:MediaDb is required. Set ConnectionStrings__MediaDb environment variable or add it to configuration.");
        }

        services.AddDbContext<MediaDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IMediaFileRepository, MediaFileRepository>();

        // S3 / MinIO client — singleton as it is thread-safe and expensive to construct
        var storageConfig = configuration.GetSection("Storage").Get<StorageSettings>()
            ?? new StorageSettings();

        services.AddSingleton<IAmazonS3>(_ =>
        {
            var credentials = new BasicAWSCredentials(storageConfig.AccessKey, storageConfig.SecretKey);
            var config = new AmazonS3Config
            {
                ServiceURL = storageConfig.ServiceUrl,
                ForcePathStyle = storageConfig.ForcePathStyle
            };
            return new AmazonS3Client(credentials, config);
        });

        services.AddScoped<IStorageService, S3StorageService>();
        services.AddScoped<IImageProcessingService, ImageProcessingService>();
        services.AddScoped<MediaDimensionBackfill>();
        services.AddSingleton<S3HealthCheck>();
        services.AddHostedService<BucketInitializer>();

        services.AddHttpContextAccessor();
        services.AddCurrentUserService();

        return services;
    }

    /// <summary>
    /// Binds upload options and keeps one entry per thumbnail width.
    /// When configuration does not list any width, <see cref="ThumbnailWidthList.Default"/> is used.
    /// </summary>
    public static IServiceCollection AddUploadSettings(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<UploadSettings>()
            .Bind(configuration.GetSection("Upload"))
            .PostConfigure(settings =>
            {
                var widths = ThumbnailWidthList.DistinctPositive(settings.ThumbnailWidths);
                settings.ThumbnailWidths = widths.Length == 0 ? ThumbnailWidthList.Default : widths;
            });

        return services;
    }
}
