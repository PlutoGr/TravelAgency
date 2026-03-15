using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Shared.Infrastructure.Tests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static async Task<IHost> CreateHostAsync(
        bool isDevelopment,
        RequestDelegate next,
        IEnumerable<IExceptionMapper>? exceptionMappers = null)
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(isDevelopment ? Environments.Development : Environments.Production);

        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(env);
                        services.AddLogging(builder => builder.AddConsole());
                        if (exceptionMappers != null)
                        {
                            foreach (var mapper in exceptionMappers)
                            {
                                services.AddSingleton<IExceptionMapper>(mapper);
                            }
                        }
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
                        app.Run(next);
                    });
            })
            .StartAsync();
        return host;
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationExceptionThrown_Returns400WithValidationProblemDetails()
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Name", "Name is required")
        };
        RequestDelegate next = _ => throw new ValidationException(failures);
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(400);
        root.GetProperty("title").GetString().Should().Be("Validation Error");
        json.Should().Contain("Email is required");
        json.Should().Contain("Name is required");
        var errors = root.TryGetProperty("errors", out var errs) ? errs : (root.TryGetProperty("Errors", out var e2) ? e2 : default);
        if (errors.ValueKind != JsonValueKind.Undefined)
        {
            errors.GetPropertyCount().Should().BeGreaterThanOrEqualTo(2);
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentExceptionThrown_Returns400BadRequest()
    {
        var message = "Invalid argument value";
        RequestDelegate next = _ => throw new ArgumentException(message);
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Bad Request");
        doc.RootElement.GetProperty("detail").GetString().Should().Be(message);
    }

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedAccessExceptionThrown_Returns401()
    {
        RequestDelegate next = _ => throw new UnauthorizedAccessException();
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Unauthorized");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("Authentication is required.");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionThrown_Returns500WithProblemDetails()
    {
        var exceptionMessage = "Something broke.";
        RequestDelegate next = _ => throw new InvalidOperationException(exceptionMessage);
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        root.GetProperty("status").GetInt32().Should().Be(500);
        root.GetProperty("title").GetString().Should().Be("Internal Server Error");
        root.GetProperty("detail").GetString().Should().Contain(exceptionMessage);
        root.TryGetProperty("traceId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionInProduction_DetailIsGenericMessage()
    {
        RequestDelegate next = _ => throw new InvalidOperationException("Secret internal detail.");
        using var host = await CreateHostAsync(isDevelopment: false, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        doc.RootElement.GetProperty("detail").GetString().Should().Be("An unexpected error occurred.");
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledExceptionInTestingEnvironment_ShowsExceptionDetail()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns("Testing");

        var message = "Testing env detail";
        RequestDelegate next = _ => throw new InvalidOperationException(message);

        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(env);
                        services.AddLogging();
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
                        app.Run(next);
                    });
            })
            .StartAsync();

        using (host)
        {
            var client = host.GetTestClient();
            var response = await client.GetAsync("/");
            var json = await response.Content.ReadAsStringAsync();
            json.Should().Contain(message);
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenCustomMapperHandlesException_UsesMapperResult()
    {
        var customDetails = new ProblemDetails
        {
            Title = "Custom Mapped",
            Status = 418,
            Detail = "I'm a teapot"
        };
        var customMapper = new TestExceptionMapper(418, customDetails);

        RequestDelegate next = _ => throw new InvalidOperationException("ignored");
        using var host = await CreateHostAsync(isDevelopment: true, next, new[] { customMapper });
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be((HttpStatusCode)418);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Custom Mapped");
        doc.RootElement.GetProperty("detail").GetString().Should().Be("I'm a teapot");
    }

    private sealed class TestExceptionMapper(int statusCode, ProblemDetails details) : IExceptionMapper
    {
        public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
        {
            result = (statusCode, details);
            return true;
        }
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseHasAlreadyStarted_DoesNotWriteProblemDetails()
    {
        const string bodySent = "already-sent";
        RequestDelegate next = async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(bodySent);
            throw new InvalidOperationException("Too late.");
        };
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(bodySent);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().NotContain("Internal Server Error");
        response.Content.Headers.ContentType?.MediaType.Should().NotBe("application/problem+json");
    }

    [Fact]
    public async Task InvokeAsync_ProblemDetailsContainsInstanceAndTraceId()
    {
        RequestDelegate next = _ => throw new Exception("Any error.");
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/items");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("instance").GetString().Should().Be("/api/items");
        root.TryGetProperty("traceId", out var traceId).Should().BeTrue();
        traceId.ValueKind.Should().Be(JsonValueKind.String);
    }
}
