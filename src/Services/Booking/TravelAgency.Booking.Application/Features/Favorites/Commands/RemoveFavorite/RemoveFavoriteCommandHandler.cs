using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Favorites.Commands.RemoveFavorite;

public sealed class RemoveFavoriteCommandHandler(
    ICurrentUserService currentUser,
    IFavoriteRepository favoriteRepository,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RemoveFavoriteCommand, Unit>
{
    public async Task<Unit> Handle(RemoveFavoriteCommand command, CancellationToken cancellationToken)
    {
        var favorite = await favoriteRepository.GetAsync(currentUser.UserId, command.TourId, cancellationToken)
            ?? throw new NotFoundException($"Favorite for tour '{command.TourId}' was not found.");

        favoriteRepository.Remove(favorite);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
