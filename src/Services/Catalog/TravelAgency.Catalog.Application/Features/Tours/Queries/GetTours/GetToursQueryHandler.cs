using MediatR;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Interfaces;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

public class GetToursQueryHandler : IRequestHandler<GetToursQuery, PagedResult<TourSummaryDto>>
{
    private readonly ITourRepository _tourRepository;

    public GetToursQueryHandler(ITourRepository tourRepository) => _tourRepository = tourRepository;

    public Task<PagedResult<TourSummaryDto>> Handle(GetToursQuery request, CancellationToken ct)
        => _tourRepository.GetPagedAsync(request.Filter, ct);
}
