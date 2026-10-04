using MediatR;

namespace TravelAgency.Media.Application.Features.Files;

public sealed record GetManagedMediaFileQuery(Guid Id, string Size) : IRequest<MediaFileStream>;
