using System.Text.Json.Serialization;
using MediatR;

namespace TravelAgency.Media.Application.Features.Presign;

/// <summary>
/// Request to generate a presigned URL for media download.
/// </summary>
/// <param name="Id">Media file ID.</param>
/// <param name="TtlSeconds">Optional TTL in seconds (60–604800). Uses config default when null.</param>
public sealed record PresignMediaQuery(
    [property: JsonPropertyName("mediaId")] Guid Id,
    [property: JsonPropertyName("ttlSeconds")] int? TtlSeconds = null
) : IRequest<PresignMediaResponse>;

public sealed record PresignMediaResponse(string Url, DateTimeOffset ExpiresAt);
