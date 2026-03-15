using Microsoft.AspNetCore.Mvc;

namespace TravelAgency.Shared.Infrastructure.Middleware;

/// <summary>
/// Allows domain-specific exception-to-ProblemDetails mappings.
/// Register implementations to handle custom exception types in GlobalExceptionHandlerMiddleware.
/// </summary>
public interface IExceptionMapper
{
    /// <summary>
    /// Attempts to map an exception to a status code and ProblemDetails.
    /// </summary>
    /// <param name="exception">The exception to map.</param>
    /// <param name="context">The HTTP context.</param>
    /// <param name="result">The mapped result when successful.</param>
    /// <returns>True if the exception was mapped; false to fall through to default handling.</returns>
    bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result);
}
