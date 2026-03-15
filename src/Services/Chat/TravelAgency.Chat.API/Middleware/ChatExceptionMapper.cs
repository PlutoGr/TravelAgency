using Microsoft.AspNetCore.Mvc;
using TravelAgency.Chat.Application.Exceptions;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Chat.API.Middleware;

internal sealed class ChatExceptionMapper : IExceptionMapper
{
    public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
    {
        if (exception is not AppException appEx)
        {
            result = default;
            return false;
        }

        var (type, title) = appEx.StatusCode switch
        {
            401 => ("https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized"),
            403 => ("https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden"),
            404 => ("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
            409 => ("https://tools.ietf.org/html/rfc7231#section-6.5.8", "Conflict"),
            _ => ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "Application Error")
        };

        result = (appEx.StatusCode, new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = appEx.StatusCode,
            Detail = appEx.Message,
            Instance = context.Request.Path
        });
        return true;
    }
}
