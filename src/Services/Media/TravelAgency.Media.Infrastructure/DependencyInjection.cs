using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Media.Application;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Media.Infrastructure.Repositories;
using TravelAgency.Media.Infrastructure.Services;
using TravelAgency.Media.Infrastructure.Storage;

namespace TravelAgency.Media.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMediaInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMediaApplication();

        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.Configure<UploadSettings>(configuration.GetSection("Upload"));

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

        services.AddSingleton<IMediaFileRepository, InMemoryMediaFileRepository>();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }
}
