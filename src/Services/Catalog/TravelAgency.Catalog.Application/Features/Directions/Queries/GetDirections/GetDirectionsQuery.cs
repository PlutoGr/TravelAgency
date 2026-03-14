using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Directions.Queries.GetDirections;

public record GetDirectionsQuery : IRequest<List<DirectionDto>>;
