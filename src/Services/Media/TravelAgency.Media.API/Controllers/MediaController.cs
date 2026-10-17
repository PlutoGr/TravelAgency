using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Media.Application.Features.Delete;
using TravelAgency.Media.Application.Features.Get;
using TravelAgency.Media.Application.Features.Presign;
using TravelAgency.Media.Application.Features.Upload;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Media.API.Controllers;

[ApiController]
[Route("media")]
[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public sealed class MediaController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Upload a file. Returns the media id and metadata, without a storage address.
    /// The caller builds /api/v1/media/files/{id}/{size} or the manage path for a draft.
    /// Supports images (JPEG, PNG, WebP, GIF) and PDF.
    /// Thumbnails are auto-generated for images.
    /// </summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadMediaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload(IFormFile file, [FromQuery] string? purpose, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var command = new UploadMediaCommand(
            stream,
            file.FileName,
            file.ContentType,
            file.Length,
            purpose);

        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Download the original file.
    /// A public tour-image is readable by any authenticated user.
    /// Any other file, including a public file with another purpose, is readable
    /// by its owner and by Manager or Admin.
    /// Anyone else gets 404, the same response as a missing or deleted file.
    /// A response that is not a public tour-image is sent with Cache-Control: private, no-store.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        // User id and role come from the same ICurrentUserService as GetManagedMediaFileQueryHandler.
        var roles = string.IsNullOrEmpty(currentUser.Role)
            ? Array.Empty<string>()
            : new[] { currentUser.Role };

        var result = await mediator.Send(new GetMediaQuery(id, currentUser.UserId, roles), ct);
        if (!result.IsPublicTourImage)
            Response.Headers.CacheControl = MediaCacheControl.PrivateNoStore;

        Response.OnCompleted(async () => await result.Content.DisposeAsync());
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
