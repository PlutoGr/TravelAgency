using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace TravelAgency.Shared.Infrastructure.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request handling and elapsed time.
/// Exceptions are not logged here, only rethrown; see GlobalExceptionHandlerMiddleware.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        // No catch here: a failed request is logged once at the HTTP boundary
        // (GlobalExceptionHandlerMiddleware), 4xx as Warning without stack, 5xx as Error (issue #57).
        var response = await next(cancellationToken);
        stopwatch.Stop();

        logger.LogInformation(
            "Handled {RequestName} in {ElapsedMs}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
