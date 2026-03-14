using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTour;

public record UpdateTourCommand(Guid Id, UpdateTourRequest Request) : IRequest<TourDto>;
