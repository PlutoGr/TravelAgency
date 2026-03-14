using MediatR;
using TravelAgency.Chat.Application.DTOs;

namespace TravelAgency.Chat.Application.Features.Messages.Queries.GetMessages;

public record GetMessagesQuery(Guid BookingId) : IRequest<IReadOnlyList<ChatMessageDto>>;
