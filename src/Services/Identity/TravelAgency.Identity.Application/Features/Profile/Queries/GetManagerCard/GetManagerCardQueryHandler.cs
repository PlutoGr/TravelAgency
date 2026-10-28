using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Profile.Queries.GetManagerCard;

public sealed class GetManagerCardQueryHandler(IUserRepository userRepository)
    : IRequestHandler<GetManagerCardQuery, ManagerPublicCardDto>
{
    public async Task<ManagerPublicCardDto> Handle(GetManagerCardQuery request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);

        // Один и тот же 404 для неизвестного id и для клиента: 403 подтвердил бы чужой аккаунт.
        if (user is null || user.Role is not (UserRole.Manager or UserRole.Admin))
            throw new NotFoundException("User not found.");

        return ProfileMapper.ToPublicCard(user);
    }
}
