using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;

namespace TravelAgency.Booking.Application.Features.Favorites.Queries.GetMyFavorites;

public sealed class GetMyFavoritesQueryHandler(
    ICurrentUserService currentUser,
    IFavoriteRepository favoriteRepository)
    : IRequestHandler<GetMyFavoritesQuery, IReadOnlyList<FavoriteDto>>
{
    public async Task<IReadOnlyList<FavoriteDto>> Handle(GetMyFavoritesQuery query, CancellationToken cancellationToken)
    {
        var favorites = await favoriteRepository.GetByUserIdAsync(currentUser.UserId, cancellationToken);
        return favorites.Select(f => f.ToDto()).ToList().AsReadOnly();
    }
}
