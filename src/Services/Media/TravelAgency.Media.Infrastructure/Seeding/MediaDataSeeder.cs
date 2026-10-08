using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Infrastructure.Persistence;
using TravelAgency.Shared.Contracts.Seeding;
using TravelAgency.Shared.Infrastructure.Seeding;

namespace TravelAgency.Media.Infrastructure.Seeding;

/// <summary>
/// Четыре демо-фото на каждый опубликованный тур. Файлы с фиксированными id
/// уходят в то же хранилище, что и обычная загрузка. Повторный запуск не плодит строки,
/// отсутствующий объект создаётся заново.
/// </summary>
public sealed class MediaDataSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IConfiguration configuration,
    ILogger<MediaDataSeeder> logger) : IHostedService
{
    private static readonly string[] Captions = ["MALDIVES", "PHUKET", "SANTORINI", "BALI", "DUBAI"];

    public static bool ShouldSeed(IHostEnvironment environment, IConfiguration configuration) =>
        DemoSeedGate.ShouldSeed(environment, configuration, includeDemoCatalogFlag: true);

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!ShouldSeed(environment, configuration))
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MediaDbContext>();
            var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var images = scope.ServiceProvider.GetRequiredService<IImageProcessingService>();
            await SeedAsync(db, storage, images, logger, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Media seed failed (non-fatal)");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public static async Task SeedAsync(
        MediaDbContext db,
        IStorageService storage,
        IImageProcessingService images,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var ownerId = DemoSeedIds.ManagerId.ToString();
        for (var tourNumber = 1; tourNumber <= DemoSeedIds.PublishedTourCount; tourNumber++)
        {
            for (var photoNumber = 1; photoNumber <= DemoSeedIds.PhotosPerTour; photoNumber++)
            {
                var id = DemoSeedIds.PhotoId(tourNumber, photoNumber);
                if (await KeepExistingAsync(db, storage, id, cancellationToken))
                    continue;

                var caption = $"{Captions[tourNumber - 1]} {photoNumber}";
                var bytes = DemoTourImage.Create(caption, tourNumber * 10 + photoNumber);
                var storageKey = $"{DemoSeedIds.ManagerId:N}/{id:N}/photo.webp";
                await using (var original = new MemoryStream(bytes, writable: false))
                {
                    await storage.UploadAsync(original, storageKey, DemoTourImage.ContentType, cancellationToken);
                }

                var file = MediaFile.Create(
                    $"photo-{tourNumber}-{photoNumber}.webp",
                    DemoTourImage.ContentType,
                    bytes.Length,
                    storageKey,
                    ownerId,
                    MediaPurposes.TourImage,
                    DemoTourImage.Width,
                    DemoTourImage.Height,
                    id);

                await using var source = new MemoryStream(bytes, writable: false);
                foreach (var (code, maxWidth) in TourImageSizes.All)
                {
                    source.Position = 0;
                    var resized = await images.ResizeWithinAsync(source, maxWidth, cancellationToken);
                    await using (resized.Content)
                    {
                        var previewKey = $"{storageKey}-preview-{code}";
                        await storage.UploadAsync(resized.Content, previewKey, resized.ContentType, cancellationToken);
                        file.AddThumbnail(previewKey, resized.Width, resized.Height, code);
                    }
                }

                file.MarkPublic();
                db.MediaFiles.Add(file);
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Seeded demo photo {MediaFileId}", id);
            }
        }
    }

    private static async Task<bool> KeepExistingAsync(
        MediaDbContext db,
        IStorageService storage,
        Guid id,
        CancellationToken cancellationToken)
    {
        var existing = await db.MediaFiles
            .Include(f => f.Thumbnails)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (existing is null)
            return false;

        if (await ObjectExistsAsync(storage, existing.StorageKey, cancellationToken))
        {
            if (!existing.IsPublic)
            {
                existing.MarkPublic();
                await db.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        db.MediaFiles.Remove(existing);
        await db.SaveChangesAsync(cancellationToken);
        return false;
    }

    private static async Task<bool> ObjectExistsAsync(IStorageService storage, string key, CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await storage.DownloadAsync(key, cancellationToken);
            return stream is not null;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
