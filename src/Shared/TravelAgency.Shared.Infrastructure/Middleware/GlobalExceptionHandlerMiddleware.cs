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
    /// FluentValidation names its built-in rules after the validator class
    /// (for example GreaterThanOrEqualValidator). Only codes we assigned ourselves
    /// are copied into the response; every other code becomes "validation".
    /// </summary>
    private static readonly HashSet<string> OwnedValidationCodes = new(StringComparer.Ordinal)
    {
        "date_range",
    };

    /// <summary>
    /// The single place where a failed HTTP request is logged (issues #57, #13 and #198):
    /// expected client errors (401, 403, 404, validation and any other 4xx) are logged once as Warning
    /// without the exception object, so there is no stack trace.
    /// The exception message is included only for our AppException; other 4xx log the exception type only.
    /// 5xx and unmapped exceptions are logged once as Error with the exception.
    /// MediatR behaviors only rethrow and do not log the exception again.
    /// The same middleware is used by Identity, Catalog, Booking, Chat, Media and Gateway.
    /// A cancelled request is logged as Information without the exception, before this mapping.
    /// </summary>
    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;

        if (IsClientCancellation(context, exception))
        {
            // No exception object and no message: a nested provider exception must not print a stack or its text.
            _logger.LogInformation(
                "Request cancelled {Method} {Path} {ExceptionType} traceId={TraceId}",
                context.Request.Method,
                context.Request.Path,
                exception.GetType().Name,
                correlationId);
            return;
        }

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
            // ex.Message only for our AppException (Guid and enum). Other 4xx keep the type,
            // so framework text (ArgumentException, Enum.Parse) cannot repeat user input.
            if (IsOwnAppException(exception))
            {
                _logger.LogWarning(
                    "{Method} {Path} {StatusCode} {ExceptionType}: {ExceptionMessage} traceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    exception.GetType().Name,
                    exception.Message,
                    correlationId);
            }
            else
            {
                _logger.LogWarning(
                    "{Method} {Path} {StatusCode} {ExceptionType} traceId={TraceId}",
                    context.Request.Method,
                    context.Request.Path,
                    statusCode,
                    exception.GetType().Name,
                    correlationId);
            }

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

    /// <summary>
    /// Identity, Booking and Chat each declare their own AppException.
    /// Shared infrastructure does not reference those assemblies, so the match is by type name
    /// inside the TravelAgency namespace, including subclasses such as UnauthorizedException.
    /// </summary>
    private static bool IsOwnAppException(Exception exception)
    {
        for (var type = exception.GetType(); type is not null && type != typeof(Exception); type = type.BaseType)
        {
            if (type.Name == "AppException"
                && type.Namespace is not null
                && type.Namespace.StartsWith("TravelAgency.", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// The client already dropped the request. OperationCanceledException here is not a failure,
    /// even when a provider exception is nested inside it.
    /// </summary>
    private static bool IsClientCancellation(HttpContext context, Exception exception) =>
        context.RequestAborted.IsCancellationRequested && exception is OperationCanceledException;

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
            return CreateDatabaseProblemDetails(context);

        return exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(context, validationEx),
            ArgumentException argEx => CreateBadRequestProblemDetails(context, argEx),
            UnauthorizedAccessException => CreateUnauthorizedProblemDetails(context),
            _ => CreateInternalProblemDetails(context, exception)
        };
    }

    /// <summary>
    /// Provider failures are recognized by type only: DbException, or a type in the Npgsql namespace.
    /// The message is not inspected. Every such failure is 500 with a fixed detail.
    /// </summary>
    private static bool IsDatabaseException(Exception exception)
    {
        foreach (var current in Flatten(exception))
        {
            if (current is DbException || IsNpgsqlType(current.GetType()))
                return true;
        }

        return false;
    }

    private static bool IsNpgsqlType(Type type)
    {
        var ns = type.Namespace;
        return ns is not null
            && (ns == "Npgsql" || ns.StartsWith("Npgsql.", StringComparison.Ordinal));
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

    private static ProblemDetails CreateDatabaseProblemDetails(HttpContext context)
    {
        var details = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = StatusCodes.Status500InternalServerError,
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

        problem.Extensions["code"] = ResolveValidationCode(exception.Errors.Select(failure => failure.ErrorCode));
        return problem;
    }

    private static string ResolveValidationCode(IEnumerable<string?> codes)
    {
        string? owned = null;
        foreach (var code in codes)
        {
            if (string.IsNullOrWhiteSpace(code))
                continue;

            if (!OwnedValidationCodes.Contains(code))
                return "validation";

            if (owned is null)
            {
                owned = code;
                continue;
            }

            if (!string.Equals(owned, code, StringComparison.Ordinal))
                return "validation";
        }

        return owned ?? "validation";
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
