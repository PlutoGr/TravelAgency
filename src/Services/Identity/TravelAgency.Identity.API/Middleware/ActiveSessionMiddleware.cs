using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Infrastructure.Authorization;

namespace TravelAgency.Identity.API.Middleware;

/// <summary>
/// Отклоняет access-токен, если пользователь заблокирован или версия сессии в токене устарела.
/// Пользователя без строки в базе не трогает: так остаются рабочими служебные токены тестов.
/// </summary>
public sealed class ActiveSessionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUserRepository users)
    {
        if (context.User.Identity?.IsAuthenticated != true
            || SessionRevocationPaths.IsCredentialExchange(context.Request.Path.Value))
        {
            await next(context);
            return;
        }

        var userId = SessionRevocationMiddleware.ReadUserId(context.User);
        if (userId == Guid.Empty)
        {
            await next(context);
            return;
        }

        var user = await users.GetByIdAsync(userId, context.RequestAborted);
        if (user is not null
            && (user.IsBlocked || user.SessionVersion != TravelAgency.Shared.Contracts.Authorization.SessionRevocationNames.ReadVersion(context.User)))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
