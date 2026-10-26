using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Domain.Interfaces;
using SiteSettingsRow = TravelAgency.Identity.Domain.Entities.SiteSettings;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetAdminSiteSettings;

public sealed class GetAdminSiteSettingsQueryHandler(ISiteSettingsRepository repository)
    : IRequestHandler<GetAdminSiteSettingsQuery, SiteSettingsAdminDto>
{
    public async Task<SiteSettingsAdminDto> Handle(GetAdminSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var row = await repository.GetAsync(cancellationToken) ?? SiteSettingsRow.CreateDefault(SiteSettingsRow.SeedUpdatedAt);
        return SiteSettingsMapper.ToAdmin(row);
    }
}
