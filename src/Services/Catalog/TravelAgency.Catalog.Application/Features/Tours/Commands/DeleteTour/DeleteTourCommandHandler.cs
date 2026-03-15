using MediatR;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Domain.Entities;
using TravelAgency.Catalog.Domain.Interfaces;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.DeleteTour;

public class DeleteTourCommandHandler : IRequestHandler<DeleteTourCommand, Unit>
{
    private readonly ITourRepository _tourRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteTourCommandHandler(ITourRepository tourRepository, IUnitOfWork unitOfWork)
    {
        _tourRepository = tourRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteTourCommand command, CancellationToken ct)
    {
        var tour = await _tourRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException(nameof(Tour), command.Id);

        tour.Deactivate();
        _tourRepository.Update(tour);
        await _unitOfWork.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
