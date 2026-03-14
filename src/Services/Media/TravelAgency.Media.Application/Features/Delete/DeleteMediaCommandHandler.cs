using MediatR;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Domain.Enums;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Application.Features.Delete;

public sealed class DeleteMediaCommandHandler(
    IMediaFileRepository repository,
    IStorageService storage,
    ICurrentUserService currentUser
) : IRequestHandler<DeleteMediaCommand>
{
    public async Task Handle(DeleteMediaCommand request, CancellationToken ct)
    {
        var file = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new MediaNotFoundException(request.Id);

        if (file.Status == MediaFileStatus.Deleted)
            throw new MediaNotFoundException(request.Id);

        if (file.OwnerId != currentUser.UserId)
            throw new MediaAccessDeniedException(request.Id);

        file.MarkAsDeleted();
        await storage.DeleteAsync(file.StorageKey, ct);

        foreach (var thumb in file.Thumbnails)
            await storage.DeleteAsync(thumb.StorageKey, ct);

        await repository.SaveChangesAsync(ct);
    }
}
