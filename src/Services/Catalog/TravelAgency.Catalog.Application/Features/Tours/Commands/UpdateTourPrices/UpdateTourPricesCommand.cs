using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTourPrices;

public record UpdateTourPricesCommand(Guid TourId, UpdateTourPricesRequest Request) : IRequest<TourDto>;
