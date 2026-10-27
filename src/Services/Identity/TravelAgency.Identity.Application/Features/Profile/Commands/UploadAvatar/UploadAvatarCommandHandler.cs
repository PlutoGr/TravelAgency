using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UploadAvatar;

public sealed class UploadAvatarCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAvatarMediaClient avatarMediaClient) : IRequestHandler<UploadAvatarCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UploadAvatarCommand command, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var previousAvatarId = user.AvatarMediaFileId;

        var uploaded = await avatarMediaClient.UploadAsync(
            command.Content,
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            command.BearerToken,
            cancellationToken);

        user.SetAvatar(uploaded.MediaFileId);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (previousAvatarId is Guid oldId && oldId != uploaded.MediaFileId)
        {
            try
            {
                await avatarMediaClient.DeleteAsync(oldId, command.BearerToken, cancellationToken);
            }
            catch
            {
                // Profile already points at the new file; orphan cleanup can retry later.
            }
        }

        return ProfileMapper.ToDto(user);
    }
}
