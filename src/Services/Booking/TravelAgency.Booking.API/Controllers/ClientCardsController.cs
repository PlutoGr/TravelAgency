using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Features.Clients;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.API.Controllers;

[ApiController]
[Route("bookings/clients")]
[Authorize(Policy = AuthPolicies.RequireManager)]
public class ClientCardsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ClientCardsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClientListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetClientListQuery(), ct);
        NoStore();
        return Ok(result);
    }

    [HttpGet("{clientId}")]
    [ProducesResponseType(typeof(ClientCardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(string clientId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetClientCardQuery(clientId), ct);
        NoStore();
        return Ok(result);
    }

    [HttpPost("{clientId}/notes")]
    [ProducesResponseType(typeof(ClientNoteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddNote(
        string clientId,
        [FromBody] AddClientNoteRequest? request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new AddClientNoteCommand(clientId, request?.Text), ct);
        NoStore();
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPost]
    [HttpPut]
    [HttpPatch]
    [HttpDelete]
    public IActionResult ListUnsupported() => MethodNotAllowed();

    [HttpPost("{clientId}")]
    [HttpPut("{clientId}")]
    [HttpPatch("{clientId}")]
    [HttpDelete("{clientId}")]
    public IActionResult CardUnsupported(string clientId) => MethodNotAllowed();

    [HttpGet("{clientId}/notes")]
    [HttpPut("{clientId}/notes")]
    [HttpPatch("{clientId}/notes")]
    [HttpDelete("{clientId}/notes")]
    public IActionResult NoteUnsupported(string clientId) => MethodNotAllowed();

    private void NoStore() => Response.Headers.CacheControl = "no-store";

    private static StatusCodeResult MethodNotAllowed() =>
        new(StatusCodes.Status405MethodNotAllowed);
}

public sealed record AddClientNoteRequest(string? Text);
