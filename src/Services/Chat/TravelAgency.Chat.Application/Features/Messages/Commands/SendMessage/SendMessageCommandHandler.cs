using MediatR;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Shared.Contracts.Abstractions;
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
    IBookingGrpcClient bookingGrpcClient,
    ICurrentUserService currentUser,
    IChatMessageRepository messageRepository)
    : IRequestHandler<SendMessageCommand, ChatMessageDto>
{
    public async Task<ChatMessageDto> Handle(SendMessageCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;
        var canAccess = await bookingGrpcClient.ValidateBookingAccessAsync(command.BookingId, userId, cancellationToken);
        if (!canAccess)
            throw new ForbiddenException("You do not have access to this booking.");

        if (userId == Guid.Empty)
            throw new ForbiddenException("Invalid user identity.");

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
