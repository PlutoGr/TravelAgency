using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTours;

public record GetToursQuery(ToursFilterDto Filter) : IRequest<PagedResult<TourSummaryDto>>;
