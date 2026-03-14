using System.Net;
using System.Text.Json;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Domain.Exceptions;

namespace TravelAgency.Booking.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred while processing {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response already started, cannot write ProblemDetails for exception.");
            return;
        }

        context.Response.ContentType = "application/problem+json";

        var problemDetails = exception switch
        {
            AppValidationException validationEx => CreateValidationProblemDetails(context, validationEx),
            NotFoundException notFoundEx => CreateAppProblemDetails(context, notFoundEx),
            ConflictException conflictEx => CreateAppProblemDetails(context, conflictEx),
            ForbiddenException forbiddenEx => CreateAppProblemDetails(context, forbiddenEx),
            UnauthorizedException unauthorizedEx => CreateAppProblemDetails(context, unauthorizedEx),
            AppException appEx => CreateAppProblemDetails(context, appEx),
            BookingDomainException domainEx => CreateDomainProblemDetails(context, domainEx),
            RpcException => CreateServiceUnavailableProblemDetails(context),
            _ => CreateInternalProblemDetails(context, exception)
        };

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        problemDetails.Extensions["traceId"] = correlationId;
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateValidationProblemDetails(HttpContext context, AppValidationException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = { ["errors"] = exception.Errors }
        };
    }

    private static ProblemDetails CreateAppProblemDetails(HttpContext context, AppException exception)
    {
        var (type, title) = exception.StatusCode switch
        {
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

    private static ProblemDetails CreateDomainProblemDetails(HttpContext context, BookingDomainException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc4918#section-11.2",
            Title = "Unprocessable Entity",
            Status = (int)HttpStatusCode.UnprocessableEntity,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateServiceUnavailableProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.4",
            Title = "Service Unavailable",
            Status = (int)HttpStatusCode.ServiceUnavailable,
            Detail = "Upstream service unavailable",
            Instance = context.Request.Path
        };
    }

    private ProblemDetails CreateInternalProblemDetails(HttpContext context, Exception exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = (_environment.IsDevelopment() || _environment.IsEnvironment("Testing")) ? exception.ToString() : "An unexpected error occurred.",
            Instance = context.Request.Path
        };
    }
}
