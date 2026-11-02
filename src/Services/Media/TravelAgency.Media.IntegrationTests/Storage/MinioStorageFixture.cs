using Amazon.Runtime;
using Amazon.S3;
using Docker.DotNet;
using Docker.DotNet.Models;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Testcontainers.Minio;

namespace TravelAgency.Media.IntegrationTests.Storage;

/// <summary>
/// Настоящий MinIO в контейнере и Media поверх него. Хранилище и обработка картинок не подменяются,
/// поэтому тест проходит тот же путь, что и запрос на dev: http, подпись SigV4, aws-chunked.
/// Образ вытягивается до StartAsync, старт при DockerImageNotFoundException коротко повторяется:
/// создание контейнера по digest иногда опережает регистрацию образа в демоне.
/// </summary>
public sealed class MinioStorageFixture : IAsyncLifetime
{
    /// <summary>
    /// minio/minio убран из Docker Hub. Берётся coollabsio/minio того же релиза, что на dev
    /// (сборка из исходников тега RELEASE.2025-10-15T17-29-55Z, каталог docker/minio).
    /// Digest закреплён на манифест linux/amd64 этого тега: индекс тега на CI не остаётся
    /// локальным образом и даёт DockerImageNotFoundException.
    /// Перед стартом тот же pin вытягивается через Docker API и ждётся inspect; при гонке pull
    /// StartAsync повторяется. Параллельные фикстуры ждут одну загрузку, чтобы не тянуть образ сразу вдвоём.
    /// Тег: RELEASE.2025-10-15T17-29-55Z.
    /// Digest: sha256:4f75fd76598afa23919555d1363e1fb13c632d9bcd4ce8edcf21d3cca2ed0579.
    /// </summary>
    public const string MinioImage =
        "coollabsio/minio:RELEASE.2025-10-15T17-29-55Z@sha256:4f75fd76598afa23919555d1363e1fb13c632d9bcd4ce8edcf21d3cca2ed0579";

    public const string BucketName = "media-it";

    private const int MaxStartAttempts = 3;

    private const int ImageVisibleAttempts = 20;

    private static readonly SemaphoreSlim StartupGate = new(1, 1);

    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage(MinioImage)
        .Build();

    public MinioWebApplicationFactory Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await StartupGate.WaitAsync();
        try
        {
            await EnsureMinioImageAsync();
            await StartMinioAsync();
        }
        finally
        {
            StartupGate.Release();
        }

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

    /// <summary>
    /// Вытягивает pin и ждёт, пока демон отвечает на inspect по той же ссылке.
    /// Поток pull иногда заканчивается раньше, чем образ доступен для создания контейнера.
    /// </summary>
    private static async Task EnsureMinioImageAsync()
    {
        using var configuration = TestcontainersSettings.OS.DockerEndpointAuthConfig
            .GetDockerClientConfiguration(ResourceReaper.DefaultSessionId);
        using var docker = configuration.CreateClient();

        if (await MinioImageIsLocalAsync(docker))
            return;

        try
        {
            await docker.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = MinioImage },
                null,
                new Progress<JSONMessage>());
        }
        catch (DockerApiException)
        {
            // Явный pull без заголовка реестра может получить отказ Hub.
            // StartAsync повторит загрузку с учётными данными Testcontainers.
            return;
        }

        for (var attempt = 0; attempt < ImageVisibleAttempts; attempt++)
        {
            if (await MinioImageIsLocalAsync(docker))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }
    }

    private async Task StartMinioAsync()
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _minio.StartAsync();
                return;
            }
            catch (DockerImageNotFoundException) when (attempt < MaxStartAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt));
                await EnsureMinioImageAsync();
            }
        }
    }

    private static async Task<bool> MinioImageIsLocalAsync(DockerClient docker)
    {
        try
        {
            _ = await docker.Images.InspectImageAsync(MinioImage);
            return true;
        }
        catch (DockerImageNotFoundException)
        {
            return false;
        }
    }
}
