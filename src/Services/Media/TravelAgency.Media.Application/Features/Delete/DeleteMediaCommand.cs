using MediatR;

namespace TravelAgency.Media.Application.Features.Delete;

public sealed record DeleteMediaCommand(Guid Id) : IRequest;
