using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Tours.Queries.GetTourById;

public record GetTourByIdQuery(Guid Id) : IRequest<TourDto>;
