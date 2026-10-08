using MediatR;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourById;
using TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourCards;
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

    [HttpGet("cards")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicTourCardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCards([FromQuery] string[]? ids, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTourCardsQuery(ids), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PublicTourDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTour(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTourByIdQuery(id), ct);
        return Ok(result);
    }
}
