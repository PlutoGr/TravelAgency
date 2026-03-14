using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Identity.Application.Exceptions;

namespace TravelAgency.Identity.API.Middleware;

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
            AppException appEx => CreateAppProblemDetails(context, appEx),
            _ => CreateInternalProblemDetails(context, exception)
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        var json = JsonSerializer.Serialize(problemDetails, JsonOptions);
        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateValidationProblemDetails(HttpContext context, AppValidationException exception)
    {
        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = "Validation Error",
            Detail = exception.Message,
            Instance = context.Request.Path,
            Extensions = { ["errors"] = exception.Errors }
        };
    }

    private static ProblemDetails CreateAppProblemDetails(HttpContext context, AppException exception)
    {
        return new ProblemDetails
        {
            Status = exception.StatusCode,
            Title = exception.StatusCode switch
            {
                401 => "Unauthorized",
                404 => "Not Found",
                409 => "Conflict",
                _ => "Application Error"
            },
            Detail = exception.Message,
            Instance = context.Request.Path
        };
    }

    private ProblemDetails CreateInternalProblemDetails(HttpContext context, Exception exception)
    {
        return new ProblemDetails
        {
            Status = (int)HttpStatusCode.InternalServerError,
            Title = "Internal Server Error",
            Detail = _environment.IsDevelopment() ? exception.Message : "An unexpected error occurred.",
            Instance = context.Request.Path
        };
    }
}
