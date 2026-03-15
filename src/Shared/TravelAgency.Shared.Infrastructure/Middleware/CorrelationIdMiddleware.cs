using Serilog.Context;

namespace TravelAgency.Shared.Infrastructure.Middleware;

/// <summary>
/// Adds X-Correlation-Id header to requests for distributed tracing.
/// Reads from request header or generates a new GUID; echoes back in response.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var header = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = IsValidCorrelationId(header) ? header! : Guid.NewGuid().ToString("D");

        context.Items["CorrelationId"] = correlationId;
        context.TraceIdentifier = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    private static bool IsValidCorrelationId(string? value)
        => !string.IsNullOrEmpty(value)
           && value.Length <= 128
           && value.All(c => char.IsLetterOrDigit(c) || c == '-');
}
