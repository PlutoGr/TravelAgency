using Microsoft.AspNetCore.SignalR;
using TravelAgency.Shared.Contracts.Authorization;
using TravelAgency.Shared.Infrastructure.Authorization;

namespace TravelAgency.Chat.API.Hubs;

public sealed class SessionRevocationHubFilter(
    ISessionRevocationStore store,
    ChatConnectionRegistry registry) : IHubFilter
{
    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        if (await RejectIfRevokedAsync(invocationContext.Context))
            throw new HubException("Сессия завершена.");

        return await next(invocationContext);
    }

    public async Task OnConnectedAsync(HubLifetimeContext context, Func<HubLifetimeContext, Task> next)
    {
        if (await RejectIfRevokedAsync(context.Context))
            return;

        var userId = ReadUserId(context.Context);
        if (userId != Guid.Empty)
            registry.Register(userId, context.Context.ConnectionId, () => context.Context.Abort());

        await next(context);
    }

    public Task OnDisconnectedAsync(
        HubLifetimeContext context,
        Exception? exception,
        Func<HubLifetimeContext, Exception?, Task> next)
    {
        var userId = ReadUserId(context.Context);
        if (userId != Guid.Empty)
            registry.Unregister(userId, context.Context.ConnectionId);

        return next(context, exception);
    }

    private async Task<bool> RejectIfRevokedAsync(HubCallerContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated != true)
            return false;

        var userId = SessionRevocationMiddleware.ReadUserId(user);
        if (userId == Guid.Empty)
            return false;

        try
        {
            var entry = await store.GetAsync(userId, context.ConnectionAborted);
            if (!SessionRevocationNames.Rejects(entry, SessionRevocationNames.ReadVersion(user)))
                return false;
        }
        catch (Exception)
        {
            return false;
        }

        context.Abort();
        return true;
    }

    private static Guid ReadUserId(HubCallerContext context) =>
        context.User is null ? Guid.Empty : SessionRevocationMiddleware.ReadUserId(context.User);
}
