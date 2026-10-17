using MediatR;
using TravelAgency.Media.Application.Features;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Enums;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Application.Features.Get;

public sealed class GetMediaQueryHandler(
    IMediaFileRepository repository,
    IStorageService storage
) : IRequestHandler<GetMediaQuery, GetMediaResponse>
{
    public async Task<GetMediaResponse> Handle(GetMediaQuery request, CancellationToken ct)
    {
        var file = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new MediaNotFoundException(request.Id);

        if (file.Status == MediaFileStatus.Deleted)
            throw new MediaNotFoundException(request.Id);

        if (!MediaFileAccess.CanRead(file, request.UserId, request.Roles))
            throw new MediaNotFoundException(request.Id);

        var stream = await storage.DownloadAsync(file.StorageKey, ct);
        var publicTourImage = file.IsPublic && MediaPurposes.IsTourImage(file.Purpose);
        return new GetMediaResponse(
            stream,
            file.ContentType,
            file.OriginalFileName,
            file.IsPublic,
            publicTourImage);
    }
}
