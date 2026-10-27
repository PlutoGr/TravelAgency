using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Profile.Queries.GetProfile;

public sealed class GetProfileQueryHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository) : IRequestHandler<GetProfileQuery, UserProfileDto>
{
    public async Task<UserProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        var userId = currentUserService.UserId;

        var user = await userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        return ProfileMapper.ToDto(user);
    }
}
