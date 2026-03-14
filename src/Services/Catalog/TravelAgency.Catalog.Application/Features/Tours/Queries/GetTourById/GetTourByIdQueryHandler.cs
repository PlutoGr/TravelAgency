using MediatR;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourById;

public class GetTourByIdQueryHandler : IRequestHandler<GetTourByIdQuery, TourDto>
{
    private readonly ITourRepository _tourRepository;

    public GetTourByIdQueryHandler(ITourRepository tourRepository) => _tourRepository = tourRepository;

    public async Task<TourDto> Handle(GetTourByIdQuery request, CancellationToken ct)
    {
        var tour = await _tourRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Tour), request.Id);

        return TourMapper.ToDto(tour);
    }
}
