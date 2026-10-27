using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.DeleteAvatar;

public sealed class DeleteAvatarCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    IAvatarMediaClient avatarMediaClient) : IRequestHandler<DeleteAvatarCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(DeleteAvatarCommand command, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var previous = user.AvatarMediaFileId;
        user.ClearAvatar();
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (previous is Guid mediaId)
        {
            try
            {
                await avatarMediaClient.DeleteAsync(mediaId, command.BearerToken, cancellationToken);
            }
            catch
            {
                // Profile cleared; Media delete is best-effort.
            }
        }

        return ProfileMapper.ToDto(user);
    }
}
