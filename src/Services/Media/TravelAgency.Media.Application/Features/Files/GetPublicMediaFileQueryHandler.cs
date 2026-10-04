using MediatR;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Application.Features.Files;

public sealed class GetPublicMediaFileQueryHandler(
    IMediaFileRepository repository,
    IStorageService storage
) : IRequestHandler<GetPublicMediaFileQuery, MediaFileStream>
{
    public async Task<MediaFileStream> Handle(GetPublicMediaFileQuery request, CancellationToken ct)
    {
        var file = await TourImageFileLocator.RequireActiveTourImage(repository, request.Id, ct);
        if (!file.IsPublic)
            throw new MediaNotFoundException(request.Id);

        var preview = TourImageFileLocator.RequirePreview(file, request.Size);
        var stream = await storage.DownloadAsync(preview.StorageKey, ct);
        return new MediaFileStream(stream, file.ContentType);
    }
}
