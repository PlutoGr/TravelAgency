using MediatR;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Enums;
using TravelAgency.Catalog.Domain.Interfaces;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourById;

public class GetTourByIdQueryHandler : IRequestHandler<GetTourByIdQuery, PublicTourDto>
{
    private readonly ITourRepository _tourRepository;

    public GetTourByIdQueryHandler(ITourRepository tourRepository) => _tourRepository = tourRepository;

    public async Task<PublicTourDto> Handle(GetTourByIdQuery request, CancellationToken ct)
    {
        var tour = await _tourRepository.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Tour), request.Id);

        if (tour.Status != TourStatus.Published)
            throw new NotFoundException(nameof(Tour), request.Id);

        return PublicCatalogMapper.ToPublicTour(tour, DateTime.UtcNow);
    }
}
