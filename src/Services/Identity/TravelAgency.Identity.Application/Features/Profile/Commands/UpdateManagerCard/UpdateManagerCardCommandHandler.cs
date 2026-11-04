using MediatR;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Profile;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;

namespace TravelAgency.Identity.Application.Features.Profile.Commands.UpdateManagerCard;

public sealed class UpdateManagerCardCommandHandler(
    ICurrentUserService currentUserService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IRequestHandler<UpdateManagerCardCommand, ManagerPublicCardDto>
{
    public async Task<ManagerPublicCardDto> Handle(UpdateManagerCardCommand command, CancellationToken cancellationToken)
    {
        if (!currentUserService.IsAuthenticated)
            throw new UnauthorizedException("User is not authenticated.");

        var user = await userRepository.GetByIdAsync(currentUserService.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var request = command.Request;
        var tags = request.Tags.Select(ManagerCardText.NormalizeTag).ToArray();
        var now = DateTime.UtcNow;
        user.UpdateManagerCard(
            ManagerCardText.NormalizeOptional(request.Position),
            request.TourismSince,
            ManagerCardText.NormalizeOptional(request.About),
            ManagerCardText.SerializeTags(tags),
            now);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ProfileMapper.ToPublicCard(user);
    }
}
