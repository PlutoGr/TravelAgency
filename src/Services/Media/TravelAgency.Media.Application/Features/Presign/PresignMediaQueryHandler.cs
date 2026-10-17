using MediatR;
using Microsoft.Extensions.Options;
using TravelAgency.Media.Application.Interfaces;
using TravelAgency.Media.Application.Settings;
using TravelAgency.Media.Domain.Enums;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Media.Domain.Interfaces;

namespace TravelAgency.Media.Application.Features.Presign;

public sealed class PresignMediaQueryHandler(
    IMediaFileRepository repository,
    IStorageService storage,
    IOptions<StorageSettings> options
) : IRequestHandler<PresignMediaQuery, PresignMediaResponse>
{
    public async Task<PresignMediaResponse> Handle(PresignMediaQuery request, CancellationToken ct)
    {
        var file = await repository.GetByIdAsync(request.Id, ct)
            ?? throw new MediaNotFoundException(request.Id);

        if (file.Status == MediaFileStatus.Deleted)
            throw new MediaNotFoundException(request.Id);

        var ttlSeconds = request.TtlSeconds ?? (options.Value.PresignTtlMinutes * 60);
        var ttl = TimeSpan.FromSeconds(Math.Clamp(ttlSeconds, 60, 604800)); // 1 min to 7 days
        var url = await storage.GeneratePresignedUrlAsync(file.StorageKey, ttl, ct);
        return new PresignMediaResponse(url, DateTimeOffset.UtcNow.Add(ttl));
    }
}
