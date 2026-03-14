using MediatR;
using TravelAgency.Catalog.Application.DTOs;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.CreateTour;

public record CreateTourCommand(CreateTourRequest Request) : IRequest<TourDto>;
