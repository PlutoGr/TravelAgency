using MediatR;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTour;

public class UpdateTourCommandHandler : IRequestHandler<UpdateTourCommand, TourDto>
{
    private readonly ITourRepository _tourRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTourCommandHandler(ITourRepository tourRepository, IUnitOfWork unitOfWork)
    {
        _tourRepository = tourRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TourDto> Handle(UpdateTourCommand command, CancellationToken ct)
    {
        var tour = await _tourRepository.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException(nameof(Tour), command.Id);

        var req = command.Request;
        tour.Update(
            req.Title,
            req.Description,
            req.TourType,
            req.Country,
            req.DurationDays,
            req.ImageUrl,
            req.DirectionId);

        _tourRepository.Update(tour);
        await _unitOfWork.SaveChangesAsync(ct);

        return TourMapper.ToDto(tour);
    }
}
