using Microsoft.EntityFrameworkCore;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Persistence;

namespace TravelAgency.Identity.Infrastructure.Repositories;

public sealed class UserAdminAuditRepository(IdentityDbContext dbContext) : IUserAdminAuditRepository
{
    public void Stage(UserAdminAudit audit) => dbContext.UserAdminAudits.Add(audit);

    public async Task<IReadOnlyList<UserAdminAudit>> ListForTargetAsync(
        Guid targetUserId,
        int take,
        CancellationToken ct = default)
    {
        return await dbContext.UserAdminAudits.AsNoTracking()
            .Where(audit => audit.TargetUserId == targetUserId)
            .OrderByDescending(audit => audit.CreatedAt)
            .ThenByDescending(audit => audit.Id)
            .Take(take)
            .ToListAsync(ct);
    }
}
