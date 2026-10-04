using MediatR;

namespace TravelAgency.Media.Application.Features.Upload;

public sealed record UploadMediaCommand(
    Stream FileContent,
    string FileName,
    string ContentType,
    long SizeBytes,
    string? Purpose = null
) : IRequest<UploadMediaResponse>;

public sealed record UploadMediaResponse(
    Guid Id,
    string Url,
    string FileName,
    string ContentType,
    long SizeBytes,
    IReadOnlyList<ThumbnailResponse> Thumbnails,
    DateTimeOffset UploadedAt,
    int? Width = null,
    int? Height = null,
    bool? IsPublic = null
);

public sealed record ThumbnailResponse(Guid Id, int Width, int Height, string Url);
