using Amazon.Runtime;
using Amazon.S3;
using Testcontainers.Minio;

namespace TravelAgency.Media.IntegrationTests.Storage;

/// <summary>
/// Настоящий MinIO в контейнере и Media поверх него. Хранилище и обработка картинок не подменяются,
/// поэтому тест проходит тот же путь, что и запрос на dev: http, подпись SigV4, aws-chunked.
/// </summary>
public sealed class MinioStorageFixture : IAsyncLifetime
{
    /// <summary>
    /// minio/minio убран из Docker Hub, поэтому берём сборку того же релиза, что на dev
    /// (RELEASE.2025-10-15T17-29-55Z), из coollabsio/minio. Digest закреплён, чтобы тег не подменили.
    /// </summary>
    public const string MinioImage =
        "coollabsio/minio:RELEASE.2025-10-15T17-29-55Z@sha256:69b55a1c1c5dc285ce04db96689f5b2102317fc77a50680a1874ca6efd1c87f9";

    public const string BucketName = "media-it";

    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage(MinioImage)
        .Build();

    public MinioWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _minio.StartAsync();

        // Бакет создаём заранее, чтобы тест не зависел от BucketInitializer.
        using (var s3 = CreateS3Client())
        {
            await s3.PutBucketAsync(BucketName);
        }

        Factory = new MinioWebApplicationFactory(
            _minio.GetConnectionString(),
            _minio.GetAccessKey(),
            _minio.GetSecretKey(),
            BucketName);
    }

    /// <summary>Отдельный клиент теста, чтобы читать из MinIO мимо кода Media.</summary>
    public AmazonS3Client CreateS3Client() =>
        new(
            new BasicAWSCredentials(_minio.GetAccessKey(), _minio.GetSecretKey()),
            new AmazonS3Config
            {
                ServiceURL = _minio.GetConnectionString(),
                ForcePathStyle = true
            });

    public async Task DisposeAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        await _minio.DisposeAsync();
    }
}
