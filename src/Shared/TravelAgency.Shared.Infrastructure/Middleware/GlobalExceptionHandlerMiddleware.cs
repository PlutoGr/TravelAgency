using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace TravelAgency.Shared.Infrastructure.Middleware;

/// <summary>
/// Global exception handler that returns ProblemDetails for unhandled exceptions.
/// Handles FluentValidation.ValidationException by default.
/// Domain-specific mappings can be registered via <see cref="IExceptionMapper"/>.
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IEnumerable<IExceptionMapper> _exceptionMappers;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment,
        IEnumerable<IExceptionMapper>? exceptionMappers = null)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
        _exceptionMappers = exceptionMappers ?? Enumerable.Empty<IExceptionMapper>();
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// The single place where a failed HTTP request is logged (issue #57):
    /// 4xx (expected client errors such as 400/403/404/409/422/428) are logged once as Warning without stack trace;
    /// 5xx and unmapped exceptions are logged once as Error with the exception.
    /// MediatR behaviors only rethrow and do not log the exception again.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        if (context.Response.HasStarted)
        {
            _logger.LogError(exception,
                "Unhandled exception after the response started for {Method} {Path} traceId={TraceId}",
                context.Request.Method, context.Request.Path, correlationId);
            return;
        }

        var (problemDetails, mappedStatusCode) = TryMapWithCustomMappers(context, exception);
        var details = problemDetails ?? MapToProblemDetails(context, exception);

        var statusCode = mappedStatusCode ?? details.Status ?? (int)HttpStatusCode.InternalServerError;
        if (details.Status == null)
            details.Status = statusCode;

        LogException(context, exception, statusCode, correlationId);

        details.Extensions["traceId"] = correlationId;
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var json = JsonSerializer.Serialize(details, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private void LogException(HttpContext context, Exception exception, int statusCode, string correlationId)
    {
        if (statusCode is >= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError)
        {
            _logger.LogWarning(
                "{Method} {Path} {StatusCode} {ExceptionType} traceId={TraceId}",
                context.Request.Method, context.Request.Path, statusCode, exception.GetType().Name, correlationId);
            return;
        }

        _logger.LogError(exception,
            "{Method} {Path} {StatusCode} {ExceptionType} traceId={TraceId}",
            context.Request.Method, context.Request.Path, statusCode, exception.GetType().Name, correlationId);
    }

    private (ProblemDetails? Details, int? StatusCode) TryMapWithCustomMappers(HttpContext context, Exception exception)
    {
        foreach (var mapper in _exceptionMappers)
        {
            if (mapper.TryMap(exception, context, out var result))
            {
                if (result.Details.Status == null)
                    result.Details.Status = result.StatusCode;
                return (result.Details, result.StatusCode);
            }
        }

        return (null, null);
    }

    private ProblemDetails MapToProblemDetails(HttpContext context, Exception exception)
    {
        return exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(context, validationEx),
            ArgumentException argEx => CreateBadRequestProblemDetails(context, argEx),
            UnauthorizedAccessException => CreateUnauthorizedProblemDetails(context),
            _ => CreateInternalProblemDetails(context, exception)
        };
    }

    private static ProblemDetails CreateValidationProblemDetails(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateBadRequestProblemDetails(HttpContext context, ArgumentException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }

    private static ProblemDetails CreateUnauthorizedProblemDetails(HttpContext context)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Unauthorized",
            Status = StatusCodes.Status401Unauthorized,
            Detail = "Authentication is required.",
            Instance = context.Request.Path
        };
    }

    private ProblemDetails CreateInternalProblemDetails(HttpContext context, Exception exception)
    {
        var showDetails = _environment.IsDevelopment()
            || _environment.IsEnvironment("Testing");

        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = showDetails ? exception.ToString() : "An unexpected error occurred.",
            Instance = context.Request.Path
        };
    }
}
