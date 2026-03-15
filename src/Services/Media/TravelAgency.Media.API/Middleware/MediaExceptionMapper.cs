using System.Net;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Media.Application.Exceptions;
using TravelAgency.Media.Domain.Exceptions;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Media.API.Middleware;

internal sealed class MediaExceptionMapper : IExceptionMapper
{
    public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
    {
        ProblemDetails? details = exception switch
        {
            ValidationException validationEx => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation Error",
                Detail = validationEx.Message,
                Instance = context.Request.Path,
                Extensions = { ["errors"] = validationEx.Errors }
            },
            MediaNotFoundException notFoundEx => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = notFoundEx.Message,
                Instance = context.Request.Path
            },
            MediaAccessDeniedException accessDeniedEx => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = accessDeniedEx.Message,
                Instance = context.Request.Path
            },
            _ => null
        };

        result = details is not null ? ((int)(details.Status ?? (int)HttpStatusCode.InternalServerError), details) : default;
        return details is not null;
    }
}
