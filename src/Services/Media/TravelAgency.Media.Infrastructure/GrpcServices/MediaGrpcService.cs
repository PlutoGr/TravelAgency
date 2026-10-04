using Grpc.Core;
using TravelAgency.Contracts.Grpc.Media;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Enums;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Infrastructure.GrpcServices;

public sealed class MediaGrpcService(IMediaFileRepository repository) : MediaService.MediaServiceBase
{
    public const int MaxIdsPerCall = 100;

    public override async Task<GetMediaFilesResponse> GetMediaFiles(
        GetMediaFilesRequest request,
        ServerCallContext context)
    {
        EnsureBatchSize(request.Ids.Count);
        var ids = ParseIds(request.Ids);
        var files = await repository.GetByIdsAsync(ids, context.CancellationToken);

        var byId = files
            .Where(f => f.Status != MediaFileStatus.Deleted)
            .ToDictionary(f => f.Id);

        var response = new GetMediaFilesResponse();
        foreach (var id in ids)
        {
            if (!byId.TryGetValue(id, out var file))
                continue;
            response.Files.Add(ToInfo(file));
        }

        return response;
    }

    public override async Task<MarkMediaFilesPublicResponse> MarkMediaFilesPublic(
        MarkMediaFilesPublicRequest request,
        ServerCallContext context)
    {
        EnsureBatchSize(request.Ids.Count);
        var ids = ParseIds(request.Ids);
        await repository.MarkPublicAsync(ids, context.CancellationToken);
        return new MarkMediaFilesPublicResponse();
    }

    private static void EnsureBatchSize(int count)
    {
        if (count > MaxIdsPerCall)
        {
            throw new RpcException(new Status(
                StatusCode.InvalidArgument,
                $"At most {MaxIdsPerCall} ids are allowed."));
        }
    }

    private static List<Guid> ParseIds(IEnumerable<string> rawIds)
    {
        var result = new List<Guid>();
        var seen = new HashSet<Guid>();
        foreach (var raw in rawIds)
        {
            if (Guid.TryParse(raw, out var id) && seen.Add(id))
                result.Add(id);
        }

        return result;
    }

    private static MediaFileInfo ToInfo(MediaFile file)
    {
        var info = new MediaFileInfo
        {
            Id = file.Id.ToString(),
            OwnerId = file.OwnerId,
            ContentType = file.ContentType,
            Width = file.Width ?? 0,
            Height = file.Height ?? 0,
            IsPublic = file.IsPublic
        };

        foreach (var (code, _) in TourImageSizes.All)
        {
            if (file.Thumbnails.Any(t => string.Equals(t.SizeCode, code, StringComparison.Ordinal)))
                info.AvailableSizes.Add(code);
        }

        return info;
    }
}
