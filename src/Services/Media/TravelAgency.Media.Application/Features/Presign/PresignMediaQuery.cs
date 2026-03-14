using MediatR;

namespace TravelAgency.Media.Application.Features.Presign;

public sealed record PresignMediaQuery(Guid Id) : IRequest<PresignMediaResponse>;

public sealed record PresignMediaResponse(string Url, DateTimeOffset ExpiresAt);
