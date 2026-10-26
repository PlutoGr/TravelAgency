using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetSiteSettingsAudit;

public sealed class GetSiteSettingsAuditQueryHandler(ISiteSettingsRepository repository)
    : IRequestHandler<GetSiteSettingsAuditQuery, IReadOnlyList<SiteSettingsAuditItemDto>>
{
    public const int Limit = 50;

    public async Task<IReadOnlyList<SiteSettingsAuditItemDto>> Handle(
        GetSiteSettingsAuditQuery request,
        CancellationToken cancellationToken)
    {
        var rows = await repository.GetRecentAuditAsync(Limit, cancellationToken);
        return rows
            .Select(row => new SiteSettingsAuditItemDto(
                row.FieldName,
                row.OldValue,
                row.NewValue,
                row.ChangedByUserId,
                row.ChangedAt))
            .ToList();
    }
}
