using MediatR;
using TravelAgency.Booking.Application.Abstractions;
using TravelAgency.Booking.Application.DTOs;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Application.Mapping;
using TravelAgency.Booking.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Booking.Application.Features.Bookings.Queries.GetBookingById;

public sealed class GetBookingByIdQueryHandler(
    ICurrentUserService currentUser,
    IBookingRepository bookingRepository)
    : IRequestHandler<GetBookingByIdQuery, BookingDto>
{
    public async Task<BookingDto> Handle(GetBookingByIdQuery query, CancellationToken cancellationToken)
    {
        var booking = await bookingRepository.GetByIdAsync(query.BookingId, cancellationToken)
            ?? throw new NotFoundException($"Booking '{query.BookingId}' was not found.");

        if (currentUser.Role == AppRoles.Client && booking.ClientId != currentUser.UserId)
            throw new ForbiddenException("Clients can only view their own bookings.");

        return booking.ToDto();
    }
}
