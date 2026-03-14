using MediatR;
using TravelAgency.Booking.Application.DTOs;

namespace TravelAgency.Booking.Application.Features.Favorites.Commands.AddFavorite;

public record AddFavoriteCommand(Guid TourId) : IRequest<FavoriteDto>;
