using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Commands.CreateTour;
using TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTour;
using TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTourPrices;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourById;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

namespace TravelAgency.Catalog.API.Controllers;

[ApiController]
[Route("catalog/tours")]
public class ToursController : ControllerBase
{
    private readonly IMediator _mediator;

    public ToursController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TourSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTours([FromQuery] ToursFilterDto filter, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetToursQuery(filter), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TourDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTour(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTourByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(TourDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateTourCommand(request), ct);
        return CreatedAtAction(nameof(GetTour), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(TourDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTour(Guid id, [FromBody] UpdateTourRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateTourCommand(id, request), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/prices")]
    [Authorize(Policy = "ManagerOrAdmin")]
    [ProducesResponseType(typeof(TourDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTourPrices(
        Guid id, [FromBody] UpdateTourPricesRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateTourPricesCommand(id, request), ct);
        return Ok(result);
    }
}
