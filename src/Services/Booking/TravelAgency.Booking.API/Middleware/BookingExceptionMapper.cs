using System.Net;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Domain.Exceptions;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Booking.API.Middleware;

internal sealed class BookingExceptionMapper : IExceptionMapper
{
    public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
    {
        ProblemDetails? details = exception switch
        {
            TourUnavailableException unavailable => CreateTourUnavailable(context, unavailable),
            AppException appEx => CreateAppProblemDetails(context, appEx),
            BookingDomainException domainEx => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
                Title = "Unprocessable Entity",
                Status = StatusCodes.Status422UnprocessableEntity,
                Detail = domainEx.Message,
                Instance = context.Request.Path
            },
            RpcException => new ProblemDetails
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
                Title = "Service Unavailable",
                Status = StatusCodes.Status503ServiceUnavailable,
                Detail = "Upstream service unavailable",
                Instance = context.Request.Path
            },
            _ => null
        };

        result = details is not null ? ((int)(details.Status ?? 500), details) : default;
        return details is not null;
    }

    private static ProblemDetails CreateAppProblemDetails(HttpContext context, AppException exception)
    {
        var (type, title) = exception.StatusCode switch
        {
            400 => ("https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            401 => ("https://tools.ietf.org/html/rfc7235#section-3.1", "Unauthorized"),
            403 => ("https://tools.ietf.org/html/rfc7231#section-6.5.3", "Forbidden"),
            404 => ("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
            409 => ("https://tools.ietf.org/html/rfc7231#section-6.5.8", "Conflict"),
            _ => ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "Application Error")
        };

        return new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = exception.StatusCode,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateTourUnavailable(HttpContext context, TourUnavailableException exception)
    {
        var details = new ProblemDetails
        {
            Type = "https://travelagency/errors/tour-unavailable",
            Title = "Unprocessable Entity",
            Status = StatusCodes.Status422UnprocessableEntity,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        details.Extensions["code"] = TourUnavailableException.Code;
        return details;
    }
}
