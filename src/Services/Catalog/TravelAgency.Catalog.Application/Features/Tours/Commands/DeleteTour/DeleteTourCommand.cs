using MediatR;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.DeleteTour;

public record DeleteTourCommand(Guid Id) : IRequest<Unit>;
