using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.Admin.Queries.GetAdminUser;

public sealed record GetAdminUserQuery(Guid UserId) : IRequest<AdminUserDetailsDto>;

public sealed class GetAdminUserQueryHandler(
    IUserRepository userRepository,
    IUserAdminAuditRepository auditRepository)
    : IRequestHandler<GetAdminUserQuery, AdminUserDetailsDto>
{
    public async Task<AdminUserDetailsDto> Handle(GetAdminUserQuery query, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(query.UserId, cancellationToken)
            ?? throw new NotFoundException(UserAdminText.UserNotFound);

        var audit = await auditRepository.ListForTargetAsync(user.Id, UserAdminText.AuditTake, cancellationToken);
        var names = await userRepository.GetDisplayNamesAsync(
            audit.Select(item => item.ActorUserId).Distinct().ToList(),
            cancellationToken);

        var items = audit
            .Select(item => AdminUserMapper.ToAuditDto(
                item,
                names.TryGetValue(item.ActorUserId, out var name) ? name : "Неизвестный"))
            .ToList();

        return new AdminUserDetailsDto(AdminUserMapper.ToDto(user), items);
    }
}
