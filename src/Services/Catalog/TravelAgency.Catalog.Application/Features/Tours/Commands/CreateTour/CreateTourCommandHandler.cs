using MediatR;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.CreateTour;

public class CreateTourCommandHandler : IRequestHandler<CreateTourCommand, TourDto>
{
    private readonly ITourRepository _tourRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTourCommandHandler(ITourRepository tourRepository, IUnitOfWork unitOfWork)
    {
        _tourRepository = tourRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TourDto> Handle(CreateTourCommand command, CancellationToken ct)
    {
        var req = command.Request;

        if (await _tourRepository.ExistsByTitleAsync(req.Title, ct))
            throw new ConflictException($"A tour with title '{req.Title}' already exists.");

        var tour = Tour.Create(
            req.Title,
            req.Description,
            req.TourType,
            req.Country,
            req.DurationDays,
            req.ImageUrl,
            req.DirectionId);

        await _tourRepository.AddAsync(tour, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return TourMapper.ToDto(tour);
    }
}
