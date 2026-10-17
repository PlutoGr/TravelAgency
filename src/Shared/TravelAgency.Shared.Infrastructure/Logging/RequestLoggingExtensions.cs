using Serilog;
using Serilog.Events;

namespace TravelAgency.Shared.Infrastructure.Logging;

/// <summary>
/// Shared HTTP request logging for all services (issue #57).
/// One line per request; the level is chosen by <see cref="GetLevel(HttpContext, double, Exception?)"/>.
/// </summary>
public static class RequestLoggingExtensions
{
    private static readonly PathString[] HealthPathPrefixes =
    [
        new("/health"),
        new("/healthz")
    ];

    /// <summary>
    /// Adds Serilog request logging with the shared level policy:
    /// health checks below 500 are Debug,
    /// 5xx is Error,
    /// an exception attached to a response that is not an expected 4xx is Error,
    /// expected 4xx (401, 403, 404, validation and the rest of 400-499) stay Information even when an exception is attached,
    /// everything else is Information.
    /// </summary>
    public static IApplicationBuilder UseTravelAgencyRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(options => options.GetLevel = GetLevel);
    }

    /// <summary>
    /// Level for the request log line. Signature matches Serilog's <c>RequestLoggingOptions.GetLevel</c>.
    /// </summary>
    public static LogEventLevel GetLevel(HttpContext httpContext, double elapsedMs, Exception? exception)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        return GetLevel(httpContext.Request.Path, httpContext.Response.StatusCode, exception);
    }

    /// <summary>
    /// Pure level policy on path, status code and exception.
    /// </summary>
    public static LogEventLevel GetLevel(PathString path, int statusCode, Exception? exception)
    {
        if (statusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        // A wrong password is a 401 with UnauthorizedException. That must not become Error
        // just because the exception object is still attached to the request log (issue #13).
        if (exception is not null && !IsExpectedClientError(statusCode))
            return LogEventLevel.Error;

        return IsHealthCheckPath(path) ? LogEventLevel.Debug : LogEventLevel.Information;
    }

    /// <summary>
    /// True for /health, /health/live, /health/ready, /healthz and similar health endpoints.
    /// </summary>
    public static bool IsHealthCheckPath(PathString path)
    {
        foreach (var prefix in HealthPathPrefixes)
        {
            if (path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsExpectedClientError(int statusCode) =>
        statusCode is >= StatusCodes.Status400BadRequest and < StatusCodes.Status500InternalServerError;
}
