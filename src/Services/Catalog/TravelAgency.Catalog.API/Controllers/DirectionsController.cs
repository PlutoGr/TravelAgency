using MediatR;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Directions.Queries.GetDirections;

namespace TravelAgency.Catalog.API.Controllers;

[ApiController]
[Route("catalog/directions")]
public class DirectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DirectionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(List<DirectionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDirections(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetDirectionsQuery(), ct);
        return Ok(result);
    }
}
