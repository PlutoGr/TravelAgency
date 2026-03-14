using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Chat.Application.DTOs;
using TravelAgency.Chat.Application.Features.Messages.Queries.GetMessages;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Chat.API.Controllers;

[ApiController]
[Route("chat")]
[Authorize(Policy = AuthPolicies.RequireClient)]
public class ChatController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChatController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("booking/{bookingId:guid}/messages")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMessages(Guid bookingId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMessagesQuery(bookingId), ct);
        return Ok(result);
    }
}
