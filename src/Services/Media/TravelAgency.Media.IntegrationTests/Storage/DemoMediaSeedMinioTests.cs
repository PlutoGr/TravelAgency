using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SixLabors.ImageSharp;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Media.Infrastructure.Seeding;
using TravelAgency.Shared.Contracts.Seeding;

namespace TravelAgency.Media.IntegrationTests.Storage;

/// <summary>
/// Тот же сидер, что и на dev, но бакет читается мимо Media.
/// Фикстура и образ — <see cref="MinioStorageFixture"/> из #60. Этот файл не компилируется,
/// пока фикстуры нет в ветке: после rebase на main с #60 уберите Compile Remove в csproj.
/// </summary>
public class DemoMediaSeedMinioTests(MinioStorageFixture fixture) : IClassFixture<MinioStorageFixture>
{
    [Fact]
    public async Task Seed_StoresDemoWebpInMinio_AndSecondRunDoesNotDuplicate()
    {
        fixture.Factory.CreateClient();
        await SeedAsync();
        var keys = await StorageKeysAsync();
        await SeedAsync();
        var again = await StorageKeysAsync();
        again.Should().BeEquivalentTo(keys);
        again.Should().HaveCount(DemoSeedIds.PublishedTourCount * DemoSeedIds.PhotosPerTour);

        using var s3 = fixture.CreateS3Client();
        foreach (var (id, key) in again)
        {
            using var stored = await s3.GetObjectAsync(MinioStorageFixture.BucketName, key);
            using var buffer = new MemoryStream();
            await stored.ResponseStream.CopyToAsync(buffer);
            var bytes = buffer.ToArray();
            bytes.Length.Should().BeLessThanOrEqualTo(DemoTourImage.MaxBytes);
            using var image = Image.Load(bytes);
            image.Width.Should().Be(DemoSeedIds.PhotoWidthPx);
            id.Should().NotBe(Guid.Empty);
        }

        var missing = again[0];
        await s3.DeleteObjectAsync(MinioStorageFixture.BucketName, missing.Key);
        using (var scope = fixture.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            var row = await db.MediaFiles.SingleAsync(f => f.Id == missing.Id);
            db.MediaFiles.Remove(row);
            await db.SaveChangesAsync();
        }

        await SeedAsync();
        using var check = await s3.GetObjectMetadataAsync(MinioStorageFixture.BucketName, missing.Key);
        check.ContentLength.Should().BeGreaterThan(0);
    }

    private async Task SeedAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
        var images = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
        await MediaDataSeeder.SeedAsync(db, storage, images, NullLogger.Instance, CancellationToken.None);
    }

    private async Task<List<(Guid Id, string Key)>> StorageKeysAsync()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
        var files = await db.MediaFiles.AsNoTracking().Select(f => new { f.Id, f.StorageKey }).ToListAsync();
        return files.Select(f => (f.Id, f.StorageKey)).ToList();
    }
}
