using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var req = command.Request;

        user.UpdateProfile(
            req.FirstName ?? user.FirstName,
            req.LastName ?? user.LastName,
            req.Phone);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Phone,
            user.Role.ToRoleString(),
            user.CreatedAt);
    }
}
