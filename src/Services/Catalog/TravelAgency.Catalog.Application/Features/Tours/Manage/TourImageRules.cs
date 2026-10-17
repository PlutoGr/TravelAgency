using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Domain;

namespace TravelAgency.Catalog.Application.Features.Tours.Manage;

public static class TourImageRules
{
    public const string NotFound = "images.notFound";
    public const string RejectedMessage = "One or more media files were not found.";

    public static async Task<IReadOnlyDictionary<Guid, RemoteMediaFile>> LoadOwnedAsync(
        IMediaFilesClient media,
        Guid? tourOwnerId,
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
            return new Dictionary<Guid, RemoteMediaFile>();

        var files = await media.GetMediaFilesAsync(ids, cancellationToken);
        var byId = new Dictionary<Guid, RemoteMediaFile>();
        foreach (var file in files)
            byId[file.Id] = file;

        var owner = tourOwnerId?.ToString();
        foreach (var id in ids.Distinct())
        {
            if (!byId.TryGetValue(id, out var file) || !BelongsToOwner(file, owner))
                throw new TourImageRuleException(NotFound, RejectedMessage);
        }

        return byId;
    }

    private static bool BelongsToOwner(RemoteMediaFile file, string? owner) =>
        string.Equals(file.OwnerId, owner, StringComparison.OrdinalIgnoreCase);

    public static void EnsureCoverWidth(int width)
    {
        if (width <= 0 || width < TourContentLimits.CoverMinWidthPx)
        {
            throw new TourImageRuleException(
                TourPublishRequirementCodes.ImagesCoverMinWidth,
                "Cover image must be at least 1280 px wide.");
        }
    }
}
