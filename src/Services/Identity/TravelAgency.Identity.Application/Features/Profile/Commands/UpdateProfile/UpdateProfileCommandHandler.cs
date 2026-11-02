using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var req = command.Request;
        user.UpdateProfile(
            req.FirstName is null ? user.FirstName : req.FirstName.Trim(),
            req.LastName is null ? user.LastName : req.LastName.Trim(),
            ResolvePhone(req.Phone, user.Phone));

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProfileMapper.ToDto(user);
    }

    private static string? ResolvePhone(string? requested, string? current)
    {
        if (requested is null)
            return current;
        if (requested.Length == 0)
            return null;
        var trimmed = requested.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
