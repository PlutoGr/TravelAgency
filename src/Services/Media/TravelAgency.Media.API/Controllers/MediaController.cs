using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Media.Application.Features.Delete;
using TravelAgency.Media.Application.Features.Get;
using TravelAgency.Media.Application.Features.Presign;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.API.Controllers;

[ApiController]
[Route("media")]
[Authorize(Policy = AuthPolicies.RequireClient)]
public sealed class MediaController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Upload a file. Returns the media ID and a presigned URL.
    /// Supports images (JPEG, PNG, WebP, GIF) and PDF.
    /// Thumbnails are auto-generated for images.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadMediaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadMediaCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Download a file by its ID. Returns the raw file content.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetMediaQuery(id), ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Generate a presigned URL with TTL for the given media ID.
    /// </summary>
    [HttpPost("presign")]
    [ProducesResponseType(typeof(PresignMediaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Presign([FromBody] PresignMediaQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Delete a file by its ID. Only the owner can delete.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteMediaCommand(id), ct);
        return NoContent();
    }
}
