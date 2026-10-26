using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;

namespace TravelAgency.Identity.Application.Features.Admin;

public static class AdminUserMapper
{
    public static AdminUserDto ToDto(User user) =>
        new(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email,
            user.Phone,
            user.Role.ToRoleString(),
            user.IsBlocked ? UserAdminText.BlockedStatus : UserAdminText.ActiveStatus,
            user.CreatedAt,
            user.LastLoginAt,
            user.BlockedAt,
            user.BlockReason);

    public static UserAdminAuditItemDto ToAuditDto(UserAdminAudit audit, string actorName) =>
        new(
            audit.Id,
            audit.Action,
            audit.OldRole,
            audit.NewRole,
            audit.Reason,
            audit.ActorUserId,
            actorName,
            audit.CreatedAt);
}
