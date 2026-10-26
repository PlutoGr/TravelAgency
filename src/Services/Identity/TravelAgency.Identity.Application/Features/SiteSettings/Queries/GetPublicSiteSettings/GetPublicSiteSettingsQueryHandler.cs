using MediatR;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Domain.Interfaces;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetPublicSiteSettings;

public sealed class GetPublicSiteSettingsQueryHandler(
    ISiteSettingsRepository repository,
    ISiteSettingsPublicCache cache) : IRequestHandler<GetPublicSiteSettingsQuery, SiteSettingsPublicDto>
{
    public async Task<SiteSettingsPublicDto> Handle(GetPublicSiteSettingsQuery request, CancellationToken cancellationToken)
    {
        var cached = await cache.TryGetAsync(cancellationToken);
        if (cached is not null)
            return cached;

        var row = await repository.GetAsync(cancellationToken);
        var dto = SiteSettingsMapper.ToPublic(row);
        await cache.SetAsync(dto, cancellationToken);
        return dto;
    }
}
