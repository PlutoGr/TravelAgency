using MediatR;

namespace TravelAgency.Booking.Application.Features.Favorites.Commands.RemoveFavorite;

public record RemoveFavoriteCommand(Guid TourId) : IRequest<Unit>;
