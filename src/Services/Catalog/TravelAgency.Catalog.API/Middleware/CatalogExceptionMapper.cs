using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            DbUpdateConcurrencyException => new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Conflict",
                Detail = "The tour was changed. Send the current If-Match value.",
                Instance = context.Request.Path
            },
            PreconditionRequiredException precondition => new ProblemDetails
            {
                Status = StatusCodes.Status428PreconditionRequired,
                Title = "Precondition Required",
                Detail = precondition.Message,
                Instance = context.Request.Path
            },
            ForbiddenException forbidden => new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = forbidden.Message,
                Instance = context.Request.Path
            },
            MediaUnavailableException unavailable => new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Service Unavailable",
                Detail = unavailable.Message,
                Instance = context.Request.Path
            },
            TourImageRuleException imageRule => MapImageRule(imageRule, context),
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

    private static ProblemDetails MapImageRule(TourImageRuleException exception, HttpContext context)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Unprocessable Entity",
            Type = "https://travelagency/errors/tour-image",
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        details.Extensions["code"] = exception.Code;
        details.Extensions["missing"] = new[] { exception.Code };
        return details;
    }
}
