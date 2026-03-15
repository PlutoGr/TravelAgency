using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Application.Features.Messages.Commands.SendMessage;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Chat.API.Hubs;

[Authorize(Policy = AuthPolicies.RequireAuthenticated)]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IBookingGrpcClient _bookingGrpcClient;
    private readonly ICurrentUserService _currentUserService;

    public ChatHub(IMediator mediator, IBookingGrpcClient bookingGrpcClient, ICurrentUserService currentUserService)
    {
        _mediator = mediator;
        _bookingGrpcClient = bookingGrpcClient;
        _currentUserService = currentUserService;
    }

    public async Task JoinBookingGroup(Guid bookingId)
    {
        await EnsureBookingAccessAsync(bookingId);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(bookingId));
    }

    public async Task SendMessage(Guid bookingId, string text, IReadOnlyList<string>? attachments = null)
    {
        await EnsureBookingAccessAsync(bookingId);
        var message = await _mediator.Send(new SendMessageCommand(bookingId, text, attachments));
        await Clients.Group(GroupName(bookingId)).SendAsync("MessageReceived", message);
    }

    private async Task EnsureBookingAccessAsync(Guid bookingId)
    {
        var userId = _currentUserService.UserId;
        var canAccess = await _bookingGrpcClient.ValidateBookingAccessAsync(bookingId, userId);
        if (!canAccess)
        {
            throw new HubException("You do not have access to this booking.");
        }
    }

    private static string GroupName(Guid bookingId) => $"booking_{bookingId}";
}
