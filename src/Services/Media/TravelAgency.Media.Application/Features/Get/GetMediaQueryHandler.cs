using MediatR;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain;
using TravelAgency.Media.Domain.Entities;
using TravelAgency.Media.Domain.Enums;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

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

        if (!CanRead(file, request))
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

    /// <summary>
    /// Access, in order: public tour-image, owner, Manager or Admin.
    /// A public file with another purpose does not take the first branch.
    /// Anything else is the same not-found error as a missing id, so the response
    /// does not reveal that the file exists.
    /// </summary>
    private static bool CanRead(MediaFile file, GetMediaQuery request)
    {
        if (file.IsPublic && MediaPurposes.IsTourImage(file.Purpose))
            return true;

        var isOwner = string.Equals(
            file.OwnerId,
            request.UserId.ToString(),
            StringComparison.OrdinalIgnoreCase);
        if (isOwner)
            return true;

        if (HasManagerOrAdmin(request.Roles))
            return true;

        return false;
    }

    private static bool HasManagerOrAdmin(IReadOnlyList<string> roles)
    {
        foreach (var role in roles)
        {
            if (string.Equals(role, AppRoles.Manager, StringComparison.Ordinal)
                || string.Equals(role, AppRoles.Admin, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
