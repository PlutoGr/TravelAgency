using MediatR;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Interfaces;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

public class GetToursQueryHandler : IRequestHandler<GetToursQuery, PagedResult<TourSummaryDto>>
{
    private readonly ITourListQuery _tourListQuery;

    public GetToursQueryHandler(ITourListQuery tourListQuery) => _tourListQuery = tourListQuery;

    public Task<PagedResult<TourSummaryDto>> Handle(GetToursQuery request, CancellationToken ct)
        => _tourListQuery.GetPagedAsync(request.Filter, ct);
}
