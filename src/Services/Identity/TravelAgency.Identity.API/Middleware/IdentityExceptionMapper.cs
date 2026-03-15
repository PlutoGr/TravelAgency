using Microsoft.AspNetCore.Mvc;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Identity.API.Middleware;

internal sealed class IdentityExceptionMapper : IExceptionMapper
{
    public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
    {
        if (exception is not AppException appEx)
        {
            result = default;
            return false;
        }

        result = (appEx.StatusCode, CreateAppProblemDetails(context, appEx));
        return true;
    }

    private static ProblemDetails CreateAppProblemDetails(HttpContext context, AppException exception)
    {
        var title = exception.StatusCode switch
        {
            401 => "Unauthorized",
            404 => "Not Found",
            409 => "Conflict",
            _ => "Application Error"
        };

        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }
}
