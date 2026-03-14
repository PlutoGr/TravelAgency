using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.DTOs.Requests;
using TravelAgency.Booking.Application.Features.Bookings.Commands.ChangeBookingStatus;
using TravelAgency.Booking.Application.Features.Bookings.Commands.CreateBooking;
using TravelAgency.Booking.Application.Features.Bookings.Commands.CreateProposal;
using TravelAgency.Booking.Application.Features.Bookings.Queries.GetBookingById;
using TravelAgency.Booking.Application.Features.Bookings.Queries.GetMyBookings;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.API.Controllers;

[ApiController]
[Route("bookings")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BookingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = AuthPolicies.RequireClient)]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBookingCommand(request), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("my")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyBookingsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBooking(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetBookingByIdQuery(id), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(BookingDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeBookingStatusRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ChangeBookingStatusCommand(id, request), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/proposals")]
    [Authorize(Policy = AuthPolicies.RequireManager)]
    [ProducesResponseType(typeof(ProposalDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProposal(Guid id, [FromBody] CreateProposalRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateProposalCommand(id, request), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
