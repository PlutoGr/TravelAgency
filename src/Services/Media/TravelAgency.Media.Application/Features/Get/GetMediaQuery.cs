using MediatR;

namespace TravelAgency.Media.Application.Features.Get;

public sealed record GetMediaQuery(Guid Id) : IRequest<GetMediaResponse>;

public sealed record GetMediaResponse(
    Stream Content,
    string ContentType,
    string FileName
);
