using System.Collections.Concurrent;
using System.Net;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TravelAgency.Identity.API.Middleware;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Identity.IntegrationTests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static async Task<HttpClient> CreateClientThatThrowsAsync(
        Exception exceptionToThrow,
        string environment = "Development",
        CapturingLoggerProvider? logs = null)
    {
        var ex = exceptionToThrow;
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.UseEnvironment(environment);
                webBuilder.ConfigureServices(services =>
                {
                    services.AddLogging(builder =>
                    {
                        builder.SetMinimumLevel(LogLevel.Information);
                        if (logs != null)
                            builder.AddProvider(logs);
                    });
                    services.AddSingleton<IExceptionMapper, IdentityExceptionMapper>();
                });
                webBuilder.Configure(app =>
                {
                    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();
                    app.Run(_ => throw ex);
                });
            })
            .StartAsync();
        return host.GetTestClient();
    }

    [Fact]
    public async Task WhenValidationExceptionThrown_Returns400WithErrors()
    {
        var failures = new List<FluentValidation.Results.ValidationFailure>
        {
            new("Email", "Email is required")
        };
        var client = await CreateClientThatThrowsAsync(new ValidationException(failures));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Validation Error");
    }

    [Fact]
    public async Task WhenNotFoundExceptionThrown_Returns404()
    {
        var client = await CreateClientThatThrowsAsync(new NotFoundException("Resource not found"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Not Found");
    }

    [Fact]
    public async Task WhenUnauthorizedExceptionThrown_Returns401()
    {
        var client = await CreateClientThatThrowsAsync(new UnauthorizedException("Not authorized"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WhenUnauthorizedExceptionThrown_Returns401AndLogsWarningWithoutStackTrace()
    {
        var logs = new CapturingLoggerProvider();
        var thrown = new UnauthorizedException("Invalid credentials.");
        var client = await CreateClientThatThrowsAsync(thrown, logs: logs);

        var response = await client.PostAsync("/identity/login", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Warning);
        entries[0].Exception.Should().BeNull();
        entries[0].Message.Should().Contain("401")
            .And.Contain(nameof(UnauthorizedException))
            .And.Contain("Invalid credentials");
        entries[0].Message.Should().NotContain("Unhandled exception");
        logs.Entries.Should().NotContain(e => e.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task WhenGenericExceptionThrown_Returns500AndLogsErrorWithException()
    {
        var logs = new CapturingLoggerProvider();
        var thrown = new InvalidOperationException("Unexpected error");
        var client = await CreateClientThatThrowsAsync(thrown, logs: logs);

        var response = await client.GetAsync("/identity/login");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Error);
        entries[0].Exception.Should().BeSameAs(thrown);
        entries[0].Message.Should().Contain("500").And.Contain(nameof(InvalidOperationException));
    }

    [Fact]
    public async Task WhenConflictExceptionThrown_Returns409()
    {
        var client = await CreateClientThatThrowsAsync(new ConflictException("Resource conflict"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenUnhandledExceptionThrown_Returns500()
    {
        var client = await CreateClientThatThrowsAsync(new InvalidOperationException("Unexpected error"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task WhenUnhandledExceptionThrown_InDevelopment_IncludesExceptionMessage()
    {
        var client = await CreateClientThatThrowsAsync(
            new InvalidOperationException("Detailed error message"), "Development");

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Detailed error message");
    }

    [Fact]
    public async Task WhenUnhandledExceptionThrown_InProduction_DoesNotIncludeExceptionMessage()
    {
        var client = await CreateClientThatThrowsAsync(
            new InvalidOperationException("Sensitive error detail"), "Production");

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain("Sensitive error detail");
        content.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task ResponseContentType_IsApplicationProblemJson()
    {
        var client = await CreateClientThatThrowsAsync(new NotFoundException("test"));

        var response = await client.GetAsync("/");

        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Response_ContainsTraceId()
    {
        var client = await CreateClientThatThrowsAsync(new NotFoundException("test"));

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("traceId");
    }

    private static readonly string MiddlewareCategory = typeof(GlobalExceptionHandlerMiddleware).FullName!;

    private static IReadOnlyList<CapturedLogEntry> MiddlewareEntries(CapturingLoggerProvider logs) =>
        logs.Entries.Where(e => e.Category == MiddlewareCategory).ToList();

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

        public IReadOnlyList<CapturedLogEntry> Entries => _entries.ToArray();

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, _entries);

        public void Dispose()
        {
        }

        private sealed class CapturingLogger(string category, ConcurrentQueue<CapturedLogEntry> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                entries.Enqueue(new CapturedLogEntry(category, logLevel, formatter(state, exception), exception));
            }
        }
    }

    private sealed record CapturedLogEntry(string Category, LogLevel Level, string Message, Exception? Exception);
}
