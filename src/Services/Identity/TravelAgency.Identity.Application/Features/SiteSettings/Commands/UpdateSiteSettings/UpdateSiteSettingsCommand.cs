using MediatR;
using TravelAgency.Identity.Application.DTOs;

namespace TravelAgency.Identity.Application.Features.SiteSettings.Commands.UpdateSiteSettings;

public sealed record UpdateSiteSettingsCommand(UpdateSiteSettingsRequest Request) : IRequest<SiteSettingsAdminDto>;
