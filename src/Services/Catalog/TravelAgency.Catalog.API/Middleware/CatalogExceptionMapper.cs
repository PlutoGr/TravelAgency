using System.Net;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Domain.Exceptions;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Catalog.API.Middleware;

internal sealed class CatalogExceptionMapper : IExceptionMapper
{
    public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
    {
        ProblemDetails? details = exception switch
        {
            NotFoundException notFound => new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Not Found",
                Detail = notFound.Message,
                Instance = context.Request.Path
            },
            ConflictException conflict => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = conflict.Message,
                Instance = context.Request.Path
            },
            TourNotPublishableException notPublishable => MapNotPublishable(notPublishable, context),
            CatalogDomainException domain => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = domain.Message,
                Instance = context.Request.Path
            },
            _ => null
        };

        result = details is not null ? ((int)(details.Status ?? (int)HttpStatusCode.InternalServerError), details) : default;
        return details is not null;
    }

    private static ProblemDetails MapNotPublishable(TourNotPublishableException exception, HttpContext context)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Tour is not publishable",
            Type = "https://travelagency/errors/tour-not-publishable",
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        details.Extensions["missing"] = exception.Missing;
        return details;
    }
}
