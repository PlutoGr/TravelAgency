using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetPublicSiteSettings;

public sealed record GetPublicSiteSettingsQuery : IRequest<SiteSettingsPublicDto>;
