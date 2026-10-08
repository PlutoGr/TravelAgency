using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Infrastructure.Persistence;

namespace TravelAgency.Media.Infrastructure.Maintenance;

public sealed record MediaDimensionBackfillReport(int Candidates, int Updated, int Skipped);

/// <summary>
/// Fills <c>MediaFiles.Width</c> and <c>MediaFiles.Height</c> from the object already in storage.
/// Does not upload, delete, or rewrite objects, and does not change any other column.
/// </summary>
public sealed class MediaDimensionBackfill(
    MediaDbContext db,
    IStorageService storage,
    IImageProcessingService images,
    ILogger<MediaDimensionBackfill> logger)
{
    public async Task<MediaDimensionBackfillReport> RunAsync(bool dryRun, CancellationToken cancellationToken = default)
    {
        var candidates = db.MediaFiles.Where(file =>
            file.Width == null || file.Height == null || file.Width == 0 || file.Height == 0);

        if (dryRun)
        {
            var count = await candidates.CountAsync(cancellationToken);
            logger.LogInformation(
                "backfill-dimensions dry-run: {Count} media files have empty or zero dimensions.",
                count);
            return new MediaDimensionBackfillReport(count, 0, 0);
        }

        var files = await candidates
            .Select(file => new DimensionTarget(file.Id, file.StorageKey))
            .ToListAsync(cancellationToken);

        var updated = 0;
        var skipped = 0;
        foreach (var file in files)
        {
            try
            {
                var dimensions = await ReadStoredDimensionsAsync(file.StorageKey, cancellationToken);
                var written = await db.MediaFiles
                    .Where(row => row.Id == file.Id)
                    .ExecuteUpdateAsync(
                        setters => setters
                            .SetProperty(row => row.Width, dimensions.Width)
                            .SetProperty(row => row.Height, dimensions.Height),
                        cancellationToken);

                if (written == 0)
                {
                    skipped++;
                    logger.LogWarning(
                        "backfill-dimensions skipped media file {MediaFileId} ({StorageKey}): the row was not updated.",
                        file.Id,
                        file.StorageKey);
                    continue;
                }

                updated++;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                skipped++;
                logger.LogWarning(
                    ex,
                    "backfill-dimensions skipped media file {MediaFileId} ({StorageKey}).",
                    file.Id,
                    file.StorageKey);
            }
        }

        logger.LogInformation(
            "backfill-dimensions updated {Updated} media files, skipped {Skipped}.",
            updated,
            skipped);
        return new MediaDimensionBackfillReport(files.Count, updated, skipped);
    }

    private async Task<ImageDimensions> ReadStoredDimensionsAsync(string storageKey, CancellationToken cancellationToken)
    {
        await using var remote = await storage.DownloadAsync(storageKey, cancellationToken);
        await using var buffer = new MemoryStream();
        await remote.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return await images.GetDimensionsAsync(buffer, cancellationToken);
    }

    private readonly record struct DimensionTarget(Guid Id, string StorageKey);
}
