using MediatR;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Features.Favorites.Queries.GetMyFavorites;

public record GetMyFavoritesQuery : IRequest<IReadOnlyList<FavoriteDto>>;
