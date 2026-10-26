using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.SiteSettings.Commands.UpdateSiteSettings;
using TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetAdminSiteSettings;
using TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetPublicSiteSettings;
using TravelAgency.Identity.Application.Features.SiteSettings.Queries.GetSiteSettingsAudit;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.API.Controllers;

[ApiController]
[Route("identity/site-settings")]
public class SiteSettingsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(SiteSettingsPublicDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublic(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetPublicSiteSettingsQuery(), cancellationToken);
        Response.Headers.CacheControl = "public, max-age=60";
        return Ok(result);
    }

    [HttpGet("manage")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(SiteSettingsAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetManage(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await mediator.Send(new GetAdminSiteSettingsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPut("manage")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(SiteSettingsAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update([FromBody] UpdateSiteSettingsRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await mediator.Send(new UpdateSiteSettingsCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpGet("manage/audit")]
    [Authorize(Policy = AuthPolicies.RequireAdmin)]
    [ProducesResponseType(typeof(IReadOnlyList<SiteSettingsAuditItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAudit(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        var result = await mediator.Send(new GetSiteSettingsAuditQuery(), cancellationToken);
        return Ok(result);
    }
}
