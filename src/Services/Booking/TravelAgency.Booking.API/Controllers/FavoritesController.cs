using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Features.Favorites.Commands.AddFavorite;
using TravelAgency.Booking.Application.Features.Favorites.Commands.RemoveFavorite;
using TravelAgency.Booking.Application.Features.Favorites.Queries.GetMyFavorites;

namespace TravelAgency.Booking.API.Controllers;

[ApiController]
[Route("favorites")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IMediator _mediator;

    public FavoritesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FavoriteDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFavorites(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetMyFavoritesQuery(), ct);
        return Ok(result);
    }

    [HttpPost("{tourId:guid}")]
    [ProducesResponseType(typeof(FavoriteDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddFavorite(Guid tourId, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddFavoriteCommand(tourId), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpDelete("{tourId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveFavorite(Guid tourId, CancellationToken ct)
    {
        await _mediator.Send(new RemoveFavoriteCommand(tourId), ct);
        return NoContent();
    }
}
