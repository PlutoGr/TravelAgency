using MediatR;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Interfaces;

namespace TravelAgency.Catalog.Application.Features.Directions.Queries.GetDirections;

public class GetDirectionsQueryHandler : IRequestHandler<GetDirectionsQuery, List<DirectionDto>>
{
    private readonly IDirectionRepository _directionRepository;

    public GetDirectionsQueryHandler(IDirectionRepository directionRepository)
        => _directionRepository = directionRepository;

    public async Task<List<DirectionDto>> Handle(GetDirectionsQuery request, CancellationToken ct)
    {
        var directions = await _directionRepository.GetAllActiveAsync(ct);

        return directions
            .Select(d => new DirectionDto(d.Id, d.Name, d.Country, d.Description))
            .ToList();
    }
}
