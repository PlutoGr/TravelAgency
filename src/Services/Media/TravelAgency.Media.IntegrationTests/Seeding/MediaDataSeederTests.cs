using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Media.Infrastructure.Seeding;
using TravelAgency.Media.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Media.IntegrationTests.Seeding;

public class MediaDataSeederTests
{
    [Fact]
    public async Task Seed_UploadsFourWebpPhotosPerTour_AndRecreatesMissingOnes()
    {
        await using var db = await CreateDbAsync();
        var storage = new RecordingStorage();

        await MediaDataSeeder.SeedAsync(db, storage, new ImageProcessingService(), NullLogger.Instance, CancellationToken.None);
        var uploadsAfterFirst = storage.Objects.Count;
        await MediaDataSeeder.SeedAsync(db, storage, new ImageProcessingService(), NullLogger.Instance, CancellationToken.None);

        storage.Objects.Count.Should().Be(uploadsAfterFirst);
        var files = await db.MediaFiles.AsNoTracking().Include(f => f.Thumbnails).ToListAsync();
        files.Should().HaveCount(DemoSeedIds.PublishedTourCount * DemoSeedIds.PhotosPerTour);

        foreach (var tourNumber in Enumerable.Range(1, DemoSeedIds.PublishedTourCount))
        {
            for (var photo = 1; photo <= DemoSeedIds.PhotosPerTour; photo++)
            {
                var id = DemoSeedIds.PhotoId(tourNumber, photo);
                var file = files.Single(f => f.Id == id);
                file.Width.Should().Be(DemoSeedIds.PhotoWidthPx);
                file.Width.Should().BeGreaterThanOrEqualTo(1280);
                file.ContentType.Should().Be(DemoTourImage.ContentType);
                file.Purpose.Should().Be(MediaPurposes.TourImage);
                file.IsPublic.Should().BeTrue();
                file.OwnerId.Should().Be(DemoSeedIds.ManagerId.ToString());
                file.SizeBytes.Should().BeLessThanOrEqualTo(DemoTourImage.MaxBytes);
                file.Thumbnails.Select(t => t.SizeCode).Should().BeEquivalentTo(TourImageSizes.All.Select(s => s.Code));
                storage.Objects.Should().ContainKey(file.StorageKey);

                using var image = Image.Load(storage.Objects[file.StorageKey]);
                image.Width.Should().Be(DemoSeedIds.PhotoWidthPx);
            }
        }

        var missing = files[0];
        storage.Objects.Remove(missing.StorageKey);
        var tracked = await db.MediaFiles.SingleAsync(f => f.Id == missing.Id);
        db.MediaFiles.Remove(tracked);
        await db.SaveChangesAsync();

        await MediaDataSeeder.SeedAsync(db, storage, new ImageProcessingService(), NullLogger.Instance, CancellationToken.None);

        (await db.MediaFiles.CountAsync()).Should().Be(DemoSeedIds.PublishedTourCount * DemoSeedIds.PhotosPerTour);
        (await db.MediaFiles.AnyAsync(f => f.Id == missing.Id)).Should().BeTrue();
        storage.Objects.Should().ContainKey(missing.StorageKey);
    }

    [Fact]
    public async Task Production_DoesNotSeed_EvenWhenTheFlagIsSet()
    {
        await using var db = await CreateDbAsync();
        var storage = new RecordingStorage();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DemoSeedGate.SeedDataKey] = "true",
            [DemoSeedGate.DemoCatalogKey] = "true"
        }).Build();
        var seeder = new MediaDataSeeder(
            Scope(db, storage),
            new TestEnvironment(Environments.Production),
            config,
            NullLogger<MediaDataSeeder>.Instance);

        await seeder.StartAsync(CancellationToken.None);

        (await db.MediaFiles.CountAsync()).Should().Be(0);
        storage.Objects.Should().BeEmpty();
    }

    private static async Task<MediaDbContext> CreateDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<MediaDbContext>().UseSqlite(connection).Options;
        var db = new MediaDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static IServiceScopeFactory Scope(MediaDbContext db, IStorageService storage)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(storage);
        services.AddSingleton<IImageProcessingService, ImageProcessingService>();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private sealed class RecordingStorage : IStorageService
    {
        public Dictionary<string, byte[]> Objects { get; } = new(StringComparer.Ordinal);

        public async Task<string> UploadAsync(Stream content, string key, string contentType, CancellationToken ct = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);
            Objects[key] = buffer.ToArray();
            return key;
        }

        public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
        {
            if (!Objects.TryGetValue(key, out var bytes))
                throw new InvalidOperationException("missing");
            return Task.FromResult<Stream>(new MemoryStream(bytes));
        }

        public Task DeleteAsync(string key, CancellationToken ct = default)
        {
            Objects.Remove(key);
            return Task.CompletedTask;
        }

        public Task<string> GeneratePresignedUrlAsync(string key, TimeSpan ttl, CancellationToken ct = default) =>
            Task.FromResult("http://storage.local/" + key);
    }

    private sealed class TestEnvironment(string name) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = name;
        public string ApplicationName { get; set; } = "tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
