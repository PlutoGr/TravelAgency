using MediatR;

namespace TravelAgency.Media.Application.Features.Files;

public sealed record GetPublicMediaFileQuery(Guid Id, string Size) : IRequest<MediaFileStream>;
