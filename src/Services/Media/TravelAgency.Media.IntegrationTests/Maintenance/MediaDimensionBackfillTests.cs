using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Testcontainers.Minio;
using Testcontainers.PostgreSql;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Infrastructure.Maintenance;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Media.IntegrationTests.Storage;

namespace TravelAgency.Media.IntegrationTests.Maintenance;

/// <summary>
/// MinIO + Postgres. Dry-run, then two backfill runs. The command is not started with the web host.
/// </summary>
public sealed class MediaDimensionBackfillTests : IAsyncLifetime
{
    private const string BucketName = "media-backfill";
    private const string SigningKey = "test-signing-key-must-be-at-least-32-chars-long!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithDatabase("travel_media")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private readonly MinioContainer _minio = new MinioBuilder()
        .WithImage(MinioStorageFixture.MinioImage)
        .Build();

    private readonly CollectingLoggerProvider _logs = new();

    private AmazonS3Client _s3 = null!;
    private WebApplication _app = null!;

    public async Task InitializeAsync()
    {
        await Task.WhenAll(_postgres.StartAsync(), _minio.StartAsync());

        _s3 = new AmazonS3Client(
            new BasicAWSCredentials(_minio.GetAccessKey(), _minio.GetSecretKey()),
            new AmazonS3Config
            {
                ServiceURL = _minio.GetConnectionString(),
                ForcePathStyle = true
            });
        await _s3.PutBucketAsync(BucketName);

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = "Testing",
            ApplicationName = typeof(Program).Assembly.GetName().Name,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(_logs);
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:MediaDb"] = _postgres.GetConnectionString(),
            ["JwtSettings:Issuer"] = "test-issuer",
            ["JwtSettings:Audience"] = "test-audience",
            ["JwtSettings:SigningKey"] = SigningKey,
            ["JwtSettings:ValidateLifetime"] = "false",
            ["Storage:ServiceUrl"] = _minio.GetConnectionString(),
            ["Storage:AccessKey"] = _minio.GetAccessKey(),
            ["Storage:SecretKey"] = _minio.GetSecretKey(),
            ["Storage:BucketName"] = BucketName,
            ["Storage:ForcePathStyle"] = "true",
            ["ASPNETCORE_RUN_MIGRATIONS"] = "false"
        });

        Program.ConfigureServices(builder.Services, builder.Configuration);
        _app = builder.Build();

        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        await db.Database.MigrateAsync();
    }

    [Fact]
    public async Task DryRun_ThenTwoRuns_FillsOnlyEmptyDimensions_AndSecondRunChangesNothing()
    {
        var holidayPng = CreatePng(320, 180);
        var readyPng = CreatePng(16, 16);
        var zeroPng = CreatePng(80, 40);
        var brokenBytes = new byte[] { 1, 2, 3, 4 };
        var staleThumb = new byte[] { 9, 9, 9, 9 };

        Guid holidayId;
        Guid readyId;
        Guid missingId;
        Guid brokenId;
        Guid zeroId;

        await using (var scope = _app.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            var holiday = MediaFile.Create(
                "holiday.png", "image/png", holidayPng.Length, "legacy/holiday.png", "owner-a");
            holiday.AddThumbnail("legacy/holiday.png-thumb-800", 800, 0);
            holiday.MarkPublic();
            var ready = MediaFile.Create(
                "ready.png", "image/png", readyPng.Length, "legacy/ready.png", "owner-b",
                width: 640, height: 480);
            var missing = MediaFile.Create(
                "gone.png", "image/png", 10, "legacy/does-not-exist.png", "owner-c");
            var broken = MediaFile.Create(
                "broken.png", "image/png", brokenBytes.Length, "legacy/broken.png", "owner-d");
            var zero = MediaFile.Create(
                "zero.png", "image/png", zeroPng.Length, "legacy/zero.png", "owner-e",
                width: 0, height: 0);

            db.MediaFiles.AddRange(holiday, ready, missing, broken, zero);
            await db.SaveChangesAsync();
            holidayId = holiday.Id;
            readyId = ready.Id;
            missingId = missing.Id;
            brokenId = broken.Id;
            zeroId = zero.Id;
        }

        await PutAsync("legacy/holiday.png", holidayPng, "image/png");
        await PutAsync("legacy/holiday.png-thumb-800", staleThumb, "application/octet-stream");
        await PutAsync("legacy/ready.png", readyPng, "image/png");
        await PutAsync("legacy/broken.png", brokenBytes, "image/png");
        await PutAsync("legacy/zero.png", zeroPng, "image/png");

        var objectsBefore = await ReadObjectsAsync(
            "legacy/holiday.png",
            "legacy/holiday.png-thumb-800",
            "legacy/ready.png",
            "legacy/broken.png",
            "legacy/zero.png");
        var holidayBefore = await LoadAsync(holidayId);
        var readyBefore = await LoadAsync(readyId);

        var dryRun = await Program.ExecuteDimensionBackfillAsync(_app.Services, dryRun: true);

        dryRun.Candidates.Should().Be(4);
        dryRun.Updated.Should().Be(0);
        dryRun.Skipped.Should().Be(0);
        _logs.Entries.Should().NotContain(entry => entry.Level == LogLevel.Warning);
        (await CountMissingDimensionsAsync()).Should().Be(4);
        (await LoadAsync(holidayId)).Should().BeEquivalentTo(holidayBefore);
        objectsBefore.Should().BeEquivalentTo(await ReadObjectsAsync(objectsBefore.Keys.ToArray()));

        _logs.Entries.Clear();
        var first = await Program.ExecuteDimensionBackfillAsync(_app.Services, dryRun: false);

        first.Candidates.Should().Be(4);
        first.Updated.Should().Be(2);
        first.Skipped.Should().Be(2);
        _logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("legacy/does-not-exist.png", StringComparison.Ordinal));
        _logs.Entries.Should().Contain(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("legacy/broken.png", StringComparison.Ordinal));

        var holidayAfter = await LoadAsync(holidayId);
        holidayAfter.Width.Should().Be(320);
        holidayAfter.Height.Should().Be(180);
        SameExceptDimensions(holidayBefore, holidayAfter);
        holidayAfter.Thumbnails.Should().ContainSingle();
        holidayAfter.Thumbnails[0].Width.Should().Be(800);
        holidayAfter.Thumbnails[0].Height.Should().Be(0);
        holidayAfter.Thumbnails[0].StorageKey.Should().Be("legacy/holiday.png-thumb-800");

        var readyAfter = await LoadAsync(readyId);
        readyAfter.Should().BeEquivalentTo(readyBefore);

        var missingAfter = await LoadAsync(missingId);
        missingAfter.Width.Should().BeNull();
        missingAfter.Height.Should().BeNull();

        var brokenAfter = await LoadAsync(brokenId);
        brokenAfter.Width.Should().BeNull();
        brokenAfter.Height.Should().BeNull();

        var zeroAfter = await LoadAsync(zeroId);
        zeroAfter.Width.Should().Be(80);
        zeroAfter.Height.Should().Be(40);
        zeroAfter.OriginalFileName.Should().Be("zero.png");
        zeroAfter.SizeBytes.Should().Be(zeroPng.Length);
        zeroAfter.StorageKey.Should().Be("legacy/zero.png");

        (await CountMissingDimensionsAsync()).Should().Be(2);
        (await ReadObjectsAsync(objectsBefore.Keys.ToArray())).Should().BeEquivalentTo(objectsBefore);

        var afterFirst = await LoadAllAsync(holidayId, readyId, missingId, brokenId, zeroId);
        var second = await Program.ExecuteDimensionBackfillAsync(_app.Services, dryRun: false);

        second.Candidates.Should().Be(2);
        second.Updated.Should().Be(0);
        second.Skipped.Should().Be(2);
        (await LoadAllAsync(holidayId, readyId, missingId, brokenId, zeroId)).Should().BeEquivalentTo(afterFirst);
        (await ReadObjectsAsync(objectsBefore.Keys.ToArray())).Should().BeEquivalentTo(objectsBefore);
        (await CountMissingDimensionsAsync()).Should().Be(2);
    }

    public async Task DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
        _s3?.Dispose();
        await _minio.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task PutAsync(string key, byte[] bytes, string contentType)
    {
        using var stream = new MemoryStream(bytes);
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = BucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType
        });
    }

    private async Task<Dictionary<string, byte[]>> ReadObjectsAsync(params string[] keys)
    {
        var result = new Dictionary<string, byte[]>();
        foreach (var key in keys)
        {
            using var response = await _s3.GetObjectAsync(BucketName, key);
            using var buffer = new MemoryStream();
            await response.ResponseStream.CopyToAsync(buffer);
            result[key] = buffer.ToArray();
        }

        return result;
    }

    private async Task<FileRow> LoadAsync(Guid id)
    {
        var rows = await LoadAllAsync(id);
        return rows.Single();
    }

    private async Task<List<FileRow>> LoadAllAsync(params Guid[] ids)
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var files = await db.MediaFiles
            .Include(file => file.Thumbnails)
            .Where(file => ids.Contains(file.Id))
            .ToListAsync();

        return files
            .OrderBy(file => file.StorageKey)
            .Select(file => new FileRow(
                file.Id,
                file.OriginalFileName,
                file.ContentType,
                file.SizeBytes,
                file.StorageKey,
                file.OwnerId,
                file.Status.ToString(),
                file.IsPublic,
                file.Purpose,
                file.UploadedAt,
                file.Width,
                file.Height,
                file.Thumbnails
                    .OrderBy(thumb => thumb.StorageKey)
                    .Select(thumb => new ThumbRow(thumb.StorageKey, thumb.Width, thumb.Height, thumb.SizeCode))
                    .ToArray()))
            .ToList();
    }

    private async Task<int> CountMissingDimensionsAsync()
    {
        await using var scope = _app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        await db.Database.OpenConnectionAsync();
        try
        {
            await using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT COUNT(*)
                FROM "MediaFiles"
                WHERE "Width" IS NULL
                   OR "Height" IS NULL
                   OR "Width" = 0
                   OR "Height" = 0
                """;
            var scalar = await command.ExecuteScalarAsync();
            return Convert.ToInt32(scalar);
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private static void SameExceptDimensions(FileRow before, FileRow after)
    {
        after.Id.Should().Be(before.Id);
        after.OriginalFileName.Should().Be(before.OriginalFileName);
        after.ContentType.Should().Be(before.ContentType);
        after.SizeBytes.Should().Be(before.SizeBytes);
        after.StorageKey.Should().Be(before.StorageKey);
        after.OwnerId.Should().Be(before.OwnerId);
        after.Status.Should().Be(before.Status);
        after.IsPublic.Should().Be(before.IsPublic);
        after.Purpose.Should().Be(before.Purpose);
        after.UploadedAt.Should().Be(before.UploadedAt);
    }

    private static byte[] CreatePng(int width, int height)
    {
        using var image = new Image<Rgba32>(width, height);
        using var stream = new MemoryStream();
        image.SaveAsPng(stream);
        return stream.ToArray();
    }

    private sealed record FileRow(
        Guid Id,
        string OriginalFileName,
        string ContentType,
        long SizeBytes,
        string StorageKey,
        string OwnerId,
        string Status,
        bool IsPublic,
        string Purpose,
        DateTimeOffset UploadedAt,
        int? Width,
        int? Height,
        ThumbRow[] Thumbnails);

    private sealed record ThumbRow(string StorageKey, int Width, int Height, string? SizeCode);

    private sealed class CollectingLoggerProvider : ILoggerProvider
    {
        public CollectingLogger Logger { get; } = new();
        public List<(LogLevel Level, string Message)> Entries => Logger.Entries;
        public ILogger CreateLogger(string categoryName) => Logger;
        public void Dispose() { }
    }

    private sealed class CollectingLogger : ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }
}
