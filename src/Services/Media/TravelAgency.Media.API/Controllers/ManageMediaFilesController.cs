using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Media.Application.Features.Files;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.API.Controllers;

/// <summary>
/// Draft and private tour-image previews. Owner and admin only.
/// </summary>
[ApiController]
[Route("media/manage/files")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public sealed class ManageMediaFilesController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}/{size}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, string size, CancellationToken ct)
    {
        var result = await mediator.Send(new GetManagedMediaFileQuery(id, size), ct);
        Response.Headers.CacheControl = MediaCacheControl.PrivateNoStore;
        Response.OnCompleted(async () => await result.Content.DisposeAsync());
        return File(result.Content, result.ContentType);
    }
}
