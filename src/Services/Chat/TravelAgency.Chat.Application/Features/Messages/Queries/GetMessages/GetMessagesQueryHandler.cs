using MediatR;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Application.DTOs;
using TravelAgency.Chat.Application.Exceptions;
using TravelAgency.Chat.Application.Mapping;
using TravelAgency.Chat.Domain.Interfaces;

namespace TravelAgency.Chat.Application.Features.Messages.Queries.GetMessages;

public sealed class GetMessagesQueryHandler(
    IBookingAccessService bookingAccessService,
    IChatMessageRepository messageRepository)
    : IRequestHandler<GetMessagesQuery, IReadOnlyList<ChatMessageDto>>
{
    public async Task<IReadOnlyList<ChatMessageDto>> Handle(GetMessagesQuery query, CancellationToken cancellationToken)
    {
        var canAccess = await bookingAccessService.CanAccessBookingAsync(query.BookingId, ct: cancellationToken);
        if (!canAccess)
            throw new ForbiddenException("You do not have access to this booking.");

        var messages = await messageRepository.GetByBookingIdAsync(query.BookingId, cancellationToken);
        return messages.Select(m => m.ToDto()).ToList().AsReadOnly();
    }
}
