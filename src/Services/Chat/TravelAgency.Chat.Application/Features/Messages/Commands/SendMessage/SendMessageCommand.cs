using MediatR;
using TravelAgency.Chat.Application.DTOs;

namespace TravelAgency.Chat.Application.Features.Messages.Commands.SendMessage;

/// <summary>
/// Command to send a chat message for a booking.
/// </summary>
public sealed record SendMessageCommand(
    Guid BookingId,
    string Text,
    IReadOnlyList<string>? Attachments = null) : IRequest<ChatMessageDto>;
