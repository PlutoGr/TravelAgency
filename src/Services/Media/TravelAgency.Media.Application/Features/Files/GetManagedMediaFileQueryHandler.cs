using MediatR;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.Application.Features.Files;

public sealed class GetManagedMediaFileQueryHandler(
    IMediaFileRepository repository,
    IStorageService storage,
    ICurrentUserService currentUser
) : IRequestHandler<GetManagedMediaFileQuery, MediaFileStream>
{
    public async Task<MediaFileStream> Handle(GetManagedMediaFileQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
            throw new UnauthorizedAccessException("Authentication is required.");

        var file = await TourImageFileLocator.RequireActiveTourImage(repository, request.Id, ct);

        var isAdmin = string.Equals(currentUser.Role, AppRoles.Admin, StringComparison.Ordinal);
        var isOwner = string.Equals(file.OwnerId, currentUser.UserId.ToString(), StringComparison.OrdinalIgnoreCase);
        if (!isAdmin && !isOwner)
            throw new MediaAccessDeniedException(request.Id);

        var preview = TourImageFileLocator.RequirePreview(file, request.Size);
        var stream = await storage.DownloadAsync(preview.StorageKey, ct);
        return new MediaFileStream(stream, file.ContentType);
    }
}
