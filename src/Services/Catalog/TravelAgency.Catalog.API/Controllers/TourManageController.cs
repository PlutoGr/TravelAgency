using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Features.Tours.Manage;

namespace TravelAgency.Catalog.API.Controllers;

[ApiController]
[Route("catalog/manage/tours")]
[Authorize(Policy = "ManagerOrAdmin")]
public class TourManageController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TourManageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ListManagedToursQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateTourDraftRequest? request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateTourDraftCommand(request), cancellationToken);
        return CreatedTour(result);
    }

    [HttpPut("{id:guid}/basics")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Basics(Guid id, [FromBody] UpdateTourBasicsRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTourBasicsCommand(id, IfMatch(), request), cancellationToken);
        return OkTour(result);
    }

    [HttpPut("{id:guid}/description")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Description(Guid id, [FromBody] UpdateTourDescriptionRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTourDescriptionCommand(id, IfMatch(), request), cancellationToken);
        return OkTour(result);
    }

    [HttpPut("{id:guid}/program")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Program(Guid id, [FromBody] UpdateTourProgramRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTourProgramCommand(id, IfMatch(), request), cancellationToken);
        return OkTour(result);
    }

    [HttpPut("{id:guid}/conditions")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Conditions(Guid id, [FromBody] UpdateTourConditionsRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTourConditionsCommand(id, IfMatch(), request), cancellationToken);
        return OkTour(result);
    }

    [HttpPut("{id:guid}/prices")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Prices(Guid id, [FromBody] UpdateTourManagePricesRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTourManagePricesCommand(id, IfMatch(), request), cancellationToken);
        return OkTour(result);
    }

    [HttpPut("{id:guid}/images")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Images(Guid id, [FromBody] UpdateTourImagesRequest request, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTourImagesCommand(id, IfMatch(), request), cancellationToken);
        return OkTour(result);
    }

    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishTourCommand(id, IfMatch()), cancellationToken);
        return OkTour(result);
    }

    [HttpPost("{id:guid}/unpublish")]
    [ProducesResponseType(typeof(TourManageDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Unpublish(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnpublishTourCommand(id, IfMatch()), cancellationToken);
        return OkTour(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status428PreconditionRequired)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteManagedTourCommand(id, IfMatch()), cancellationToken);
        return NoContent();
    }

    private string? IfMatch()
    {
        if (!Request.Headers.TryGetValue("If-Match", out var value))
            return null;
        var header = value.ToString();
        return string.IsNullOrWhiteSpace(header) ? null : header;
    }

    private IActionResult OkTour(TourManageDto tour)
    {
        Response.Headers.ETag = tour.Etag;
        return Ok(tour);
    }

    private IActionResult CreatedTour(TourManageDto tour)
    {
        Response.Headers.ETag = tour.Etag;
        return Created($"/catalog/manage/tours/{tour.Id}", tour);
    }
}
