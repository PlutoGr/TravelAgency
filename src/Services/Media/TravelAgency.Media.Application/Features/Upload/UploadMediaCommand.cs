using MediatR;

namespace TravelAgency.Media.Application.Features.Upload;

public sealed record UploadMediaCommand(
    Stream FileContent,
    string FileName,
    string ContentType,
    long SizeBytes
) : IRequest<UploadMediaResponse>;

public sealed record UploadMediaResponse(
    Guid Id,
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes,
    IReadOnlyList<ThumbnailResponse> Thumbnails,
    DateTimeOffset UploadedAt
);

public sealed record ThumbnailResponse(Guid Id, int Width, int Height, string Url);
