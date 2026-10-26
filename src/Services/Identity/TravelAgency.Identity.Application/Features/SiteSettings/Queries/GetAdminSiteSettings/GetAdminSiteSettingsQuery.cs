using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetAdminSiteSettings;

public sealed record GetAdminSiteSettingsQuery : IRequest<SiteSettingsAdminDto>;
