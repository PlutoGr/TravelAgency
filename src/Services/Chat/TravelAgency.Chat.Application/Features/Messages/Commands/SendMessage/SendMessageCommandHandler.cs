using MediatR;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Application.DTOs;
using TravelAgency.Chat.Application.Exceptions;
using TravelAgency.Chat.Application.Mapping;
using TravelAgency.Chat.Domain.Entities;
using TravelAgency.Chat.Domain.Enums;
using TravelAgency.Chat.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Chat.Application.Features.Messages.Commands.SendMessage;

/// <summary>
/// Handles SendMessageCommand: verifies booking access, creates and persists a chat message.
/// </summary>
public sealed class SendMessageCommandHandler(
    IBookingAccessService bookingAccess,
    ICurrentUserService currentUser,
    IChatMessageRepository messageRepository)
    : IRequestHandler<SendMessageCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var canAccess = await bookingAccess.CanAccessBookingAsync(command.BookingId, ct: cancellationToken);
        if (!canAccess)
            throw new ForbiddenException("You do not have access to this booking.");

        var userId = currentUser.UserId;
        var role = currentUser.Role;
        var senderName = currentUser.DisplayName;

        var senderRole = MapRoleToSenderRole(role);

        var message = ChatMessage.Create(
            command.BookingId,
            userId.ToString(),
            senderName,
            senderRole,
            command.Text ?? string.Empty,
            command.Attachments);

        var created = await messageRepository.AddAsync(message, cancellationToken);
        return created.ToDto();
    }

    private static SenderRole MapRoleToSenderRole(string role)
    {
        return role switch
        {
            AppRoles.Client => SenderRole.Client,
            AppRoles.Manager => SenderRole.Manager,
            AppRoles.Admin => SenderRole.Admin,
            _ => SenderRole.Client
        };
    }
}
