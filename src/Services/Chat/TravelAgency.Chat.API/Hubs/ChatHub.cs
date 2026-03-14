using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using TravelAgency.Chat.Application.Abstractions;
using TravelAgency.Chat.Application.Features.Messages.Commands.SendMessage;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Chat.API.Hubs;

[Authorize(Policy = AuthPolicies.RequireClient)]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IBookingAccessService _bookingAccessService;

    public ChatHub(IMediator mediator, IBookingAccessService bookingAccessService)
    {
        _mediator = mediator;
        _bookingAccessService = bookingAccessService;
    }

    public async Task JoinBookingGroup(Guid bookingId)
    {
        var authHeader = GetAuthorizationHeader();
        var canAccess = await _bookingAccessService.CanAccessBookingAsync(bookingId, authHeader);
        if (!canAccess)
        {
            throw new HubException("You do not have access to this booking.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(bookingId));
    }

    public async Task SendMessage(Guid bookingId, string text, IReadOnlyList<string>? attachments = null)
    {
        var authHeader = GetAuthorizationHeader();
        var canAccess = await _bookingAccessService.CanAccessBookingAsync(bookingId, authHeader);
        if (!canAccess)
        {
            throw new HubException("You do not have access to this booking.");
        }

        var message = await _mediator.Send(new SendMessageCommand(bookingId, text, attachments));
        await Clients.Group(GroupName(bookingId)).SendAsync("MessageReceived", message);
    }

    private string? GetAuthorizationHeader()
    {
        var httpContext = Context.GetHttpContext();
        var header = httpContext?.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(header))
            return header;

        var token = httpContext?.Request.Query["access_token"].FirstOrDefault();
        return !string.IsNullOrEmpty(token) ? $"Bearer {token}" : null;
    }

    private static string GroupName(Guid bookingId) => $"booking_{bookingId}";
}
