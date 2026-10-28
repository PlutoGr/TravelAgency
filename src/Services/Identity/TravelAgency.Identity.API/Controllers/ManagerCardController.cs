using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Features.Profile.Queries.GetManagerCard;

namespace TravelAgency.Identity.API.Controllers;

[ApiController]
[Route("identity/users")]
public class ManagerCardController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}/manager-card")]
    [Authorize]
    [ProducesResponseType(typeof(ManagerPublicCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetManagerCardQuery(id), cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Ok(result);
    }
}
