using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetSiteSettingsAudit;

public sealed record GetSiteSettingsAuditQuery : IRequest<IReadOnlyList<SiteSettingsAuditItemDto>>;
