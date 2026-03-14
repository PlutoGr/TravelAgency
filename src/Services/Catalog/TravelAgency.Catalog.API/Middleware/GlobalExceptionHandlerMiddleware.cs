using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using TravelAgency.Catalog.Application.Exceptions;
using TravelAgency.Catalog.Domain.Exceptions;

namespace TravelAgency.Catalog.API.Middleware;

public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, problemDetails) = exception switch
        {
            NotFoundException notFound => (
                HttpStatusCode.NotFound,
                CreateProblem(StatusCodes.Status404NotFound, "Not Found", notFound.Message, context)),

            ConflictException conflict => (
                HttpStatusCode.Conflict,
                CreateProblem(StatusCodes.Status409Conflict, "Conflict", conflict.Message, context)),

            CatalogDomainException domain => (
                HttpStatusCode.BadRequest,
                CreateProblem(StatusCodes.Status400BadRequest, "Bad Request", domain.Message, context)),

            ValidationException validation => (
                HttpStatusCode.UnprocessableEntity,
                CreateValidationProblem(validation, context)),

            _ => (
                HttpStatusCode.InternalServerError,
                CreateProblem(StatusCodes.Status500InternalServerError, "Internal Server Error",
                    "An unexpected error occurred.", context))
        };

        if ((int)statusCode >= 500)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Handled exception for {Method} {Path}", context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var json = JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private static ProblemDetails CreateProblem(int status, string title, string detail, HttpContext context)
        => new()
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

    private static ValidationProblemDetails CreateValidationProblem(ValidationException exception, HttpContext context)
    {
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return new ValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status422UnprocessableEntity,
            Title = "Validation Failed",
            Detail = "One or more validation errors occurred.",
            Instance = context.Request.Path
        };
    }
}
