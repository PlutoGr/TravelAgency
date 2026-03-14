using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Entities;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Favorites.Commands.AddFavorite;

public sealed class AddFavoriteCommandHandler(
    ICurrentUserService currentUser,
    IFavoriteRepository favoriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<AddFavoriteCommand, FavoriteDto>
{
    public async Task<FavoriteDto> Handle(AddFavoriteCommand command, CancellationToken cancellationToken)
    {
        var existing = await favoriteRepository.GetAsync(currentUser.UserId, command.TourId, cancellationToken);

        if (existing is not null)
            throw new ConflictException($"Tour '{command.TourId}' is already in favorites.");

        var favorite = Favorite.Create(currentUser.UserId, command.TourId);

        favoriteRepository.Stage(favorite);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return favorite.ToDto();
    }
}
