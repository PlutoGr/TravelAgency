using System.Data.Common;
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
    /// The single place where a failed HTTP request is logged (issues #57 and #13):
    /// expected client errors (401, 403, 404, validation and any other 4xx) are logged once as Warning
    /// with the exception type and message, but without the exception object, so there is no stack trace;
    /// 5xx and unmapped exceptions are logged once as Error with the exception.
    /// MediatR behaviors only rethrow and do not log the exception again.
    /// The same middleware is used by Identity, Catalog, Booking, Chat, Media and Gateway.
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

        var json = JsonSerializer.Serialize(details, details.GetType(), JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private void LogException(HttpContext context, Exception exception, int statusCode, string correlationId)
    {
        if (IsExpectedClientError(statusCode))
        {
            // Do not pass the exception: Serilog prints {Exception} with the stack trace.
            _logger.LogWarning(
                "{Method} {Path} {StatusCode} {ExceptionType}: {ExceptionMessage} traceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                statusCode,
                exception.GetType().Name,
                exception.Message,
                correlationId);
            return;
        }

        _logger.LogError(exception,
            "{Method} {Path} {StatusCode} {ExceptionType} traceId={TraceId}",
            context.Request.Method, context.Request.Path, statusCode, exception.GetType().Name, correlationId);
    }

    /// <summary>
    /// 401, 403, 404, validation (400) and the other expected client statuses.
    /// </summary>
    private static bool IsExpectedClientError(int statusCode) =>
        statusCode is >= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError;

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
        if (IsDatabaseException(exception))
            return CreateDatabaseProblemDetails(context, DatabaseStatus(exception));

        return exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(context, validationEx),
            ArgumentException argEx => CreateBadRequestProblemDetails(context, argEx),
            UnauthorizedAccessException => CreateUnauthorizedProblemDetails(context),
            _ => CreateInternalProblemDetails(context, exception)
        };
    }

    /// <summary>
    /// Npgsql rejects an Unspecified DateTime as ArgumentException and throws DbException for provider failures.
    /// Neither the message nor the stack belongs in the response. Logging is unchanged.
    /// </summary>
    private static bool IsDatabaseException(Exception exception)
    {
        foreach (var current in Flatten(exception))
        {
            if (current is DbException)
                return true;

            var typeName = current.GetType().FullName;
            if (typeName is not null && typeName.StartsWith("Npgsql.", StringComparison.Ordinal))
                return true;

            if (current is ArgumentException or InvalidCastException or InvalidOperationException
                && ContainsDatabaseProviderText(current.Message))
                return true;
        }

        return false;
    }

    private static int DatabaseStatus(Exception exception)
    {
        foreach (var current in Flatten(exception))
        {
            if (current is DbException)
                return StatusCodes.Status500InternalServerError;

            var typeName = current.GetType().FullName;
            if (typeName is not null && typeName.StartsWith("Npgsql.", StringComparison.Ordinal))
                return StatusCodes.Status500InternalServerError;
        }

        return StatusCodes.Status400BadRequest;
    }

    private static bool ContainsDatabaseProviderText(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return false;

        return message.Contains("Npgsql", StringComparison.Ordinal)
            || message.Contains("PostgreSQL", StringComparison.Ordinal)
            || message.Contains("Kind=Unspecified", StringComparison.Ordinal);
    }

    private static IEnumerable<Exception> Flatten(Exception exception)
    {
        var stack = new Stack<Exception>();
        stack.Push(exception);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            yield return current;

            if (current is AggregateException aggregate)
            {
                for (var index = aggregate.InnerExceptions.Count - 1; index >= 0; index--)
                    stack.Push(aggregate.InnerExceptions[index]);
            }
            else if (current.InnerException is not null)
            {
                stack.Push(current.InnerException);
            }
        }
    }

    private static ProblemDetails CreateDatabaseProblemDetails(HttpContext context, int statusCode)
    {
        var isClientError = statusCode < StatusCodes.Status500InternalServerError;
        var details = new ProblemDetails
        {
            Type = isClientError
                ? "https://tools.ietf.org/html/rfc7231#section-6.5.1"
                : "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = isClientError ? "Bad Request" : "Internal Server Error",
            Status = statusCode,
            Detail = "A database error occurred.",
            Instance = context.Request.Path
        };
        details.Extensions["code"] = "database_error";
        return details;
    }

    private static ProblemDetails CreateValidationProblemDetails(HttpContext context, ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        var problem = new ValidationProblemDetails(errors)
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = StatusCodes.Status400BadRequest,
            Detail = exception.Message,
            Instance = context.Request.Path
        };

        var codes = exception.Errors
            .Select(failure => failure.ErrorCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        problem.Extensions["code"] = codes.Length == 1 ? codes[0] : "validation";
        return problem;
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
