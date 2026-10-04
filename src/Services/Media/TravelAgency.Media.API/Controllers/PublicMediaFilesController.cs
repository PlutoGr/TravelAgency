using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Media.Application.Features.Files;

namespace TravelAgency.Media.API.Controllers;

/// <summary>
/// Anonymous tour-image previews. Non-public and unknown files answer 404.
/// </summary>
[ApiController]
[Route("media/files")]
[AllowAnonymous]
public sealed class PublicMediaFilesController(IMediator mediator) : ControllerBase
{
    [HttpGet("{id:guid}/{size}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, string size, CancellationToken ct)
    {
        var result = await mediator.Send(new GetPublicMediaFileQuery(id, size), ct);
        Response.Headers.CacheControl = MediaCacheControl.PublicImmutable;
        Response.OnCompleted(async () => await result.Content.DisposeAsync());
        return File(result.Content, result.ContentType);
    }
}
