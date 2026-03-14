using MediatR;
using TravelAgency.Catalog.Application.Abstractions;
using TravelAgency.Catalog.Application.DTOs;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Application.Interfaces;
using TravelAgency.Catalog.Application.Mappings;
using TravelAgency.Catalog.Domain.Entities;

namespace TravelAgency.Catalog.Application.Features.Tours.Commands.UpdateTourPrices;

public class UpdateTourPricesCommandHandler : IRequestHandler<UpdateTourPricesCommand, TourDto>
{
    private readonly ITourRepository _tourRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTourPricesCommandHandler(ITourRepository tourRepository, IUnitOfWork unitOfWork)
    {
        _tourRepository = tourRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<TourDto> Handle(UpdateTourPricesCommand command, CancellationToken ct)
    {
        var tour = await _tourRepository.GetByIdAsync(command.TourId, ct)
            ?? throw new NotFoundException(nameof(Tour), command.TourId);

        var newPrices = command.Request.Prices
            .Select(p => TourPrice.Create(
                tour.Id,
                p.ValidFrom,
                p.ValidTo,
                p.PricePerPerson,
                p.Currency,
                p.AvailableSeats))
            .ToList();

        tour.SetPrices(newPrices);
        _tourRepository.Update(tour);
        await _unitOfWork.SaveChangesAsync(ct);

        return TourMapper.ToDto(tour);
    }
}
