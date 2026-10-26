using TravelAgency.Identity.Domain.Entities;

namespace TravelAgency.Identity.Domain.Interfaces;

public interface IUserAdminAuditRepository
{
    void Stage(UserAdminAudit audit);

    Task<IReadOnlyList<UserAdminAudit>> ListForTargetAsync(
        Guid targetUserId,
        int take,
        CancellationToken ct = default);
}
