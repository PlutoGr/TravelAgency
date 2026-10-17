using System.Data.Common;
using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TravelAgency.Shared.Infrastructure.Middleware;
using TravelAgency.Shared.Infrastructure.Tests.Helpers;

namespace TravelAgency.Shared.Infrastructure.Tests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static async Task<IHost> CreateHostAsync(
        bool isDevelopment,
        RequestDelegate next,
        IEnumerable<IExceptionMapper>? exceptionMappers = null,
        CapturingLoggerProvider? logs = null)
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
                        services.AddLogging(builder =>
                        {
                            builder.AddConsole();
                            if (logs != null)
                                builder.AddProvider(logs);
                        });
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

    // --- Logging policy (issue #57): one log entry per failed request ---

    private static readonly string MiddlewareCategory = typeof(GlobalExceptionHandlerMiddleware).FullName!;

    private static IReadOnlyList<CapturedLogEntry> MiddlewareEntries(CapturingLoggerProvider logs) =>
        logs.Entries.Where(e => e.Category == MiddlewareCategory).ToList();

    [Fact]
    public async Task InvokeAsync_WhenUnauthorizedException_Returns401AndLogsOneWarningWithoutException()
    {
        var logs = new CapturingLoggerProvider();
        var thrown = new UnauthorizedException("Invalid credentials.");
        RequestDelegate next = _ => throw thrown;
        using var host = await CreateHostAsync(
            isDevelopment: false,
            next,
            new[] { new StatusExceptionMapper(StatusCodes.Status401Unauthorized, "Unauthorized") },
            logs);
        var client = host.GetTestClient();

        var response = await client.PostAsync("/identity/login", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        var entry = entries[0];
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Exception.Should().BeNull("expected 401 is logged without stack trace");
        entry.Message.Should().Contain("POST").And.Contain("/identity/login")
            .And.Contain("401").And.Contain(nameof(UnauthorizedException))
            .And.Contain("Invalid credentials").And.Contain("traceId=");
        entry.Message.Should().NotContain("Unhandled exception");
        logs.Entries.Should().NotContain(e => e.Level >= LogLevel.Error);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status403Forbidden)]
    [InlineData(StatusCodes.Status404NotFound)]
    public async Task InvokeAsync_WhenMappedClientError_ReturnsStatusAndLogsWarningWithoutException(int statusCode)
    {
        var logs = new CapturingLoggerProvider();
        RequestDelegate next = _ => throw new ClientErrorException("expected");
        using var host = await CreateHostAsync(
            isDevelopment: true,
            next,
            new[] { new StatusExceptionMapper(statusCode, "Client error") },
            logs);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/resource");

        ((int)response.StatusCode).Should().Be(statusCode);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Warning);
        entries[0].Exception.Should().BeNull();
        entries[0].Message.Should().Contain(statusCode.ToString()).And.Contain(nameof(ClientErrorException));
        logs.Entries.Should().NotContain(e => e.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_WhenConflictException_Returns409AndLogsOneWarningWithoutException()
    {
        var logs = new CapturingLoggerProvider();
        RequestDelegate next = _ => throw new ConflictException("The tour was changed.");
        using var host = await CreateHostAsync(isDevelopment: true, next, new[] { new ConflictExceptionMapper() }, logs);
        var client = host.GetTestClient();

        var response = await client.PutAsync("/api/v1/catalog/tours/42", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        var entry = entries[0];
        entry.Level.Should().Be(LogLevel.Warning);
        entry.Exception.Should().BeNull("expected 4xx are logged without stack trace");
        entry.Message.Should().Contain("PUT").And.Contain("/api/v1/catalog/tours/42")
            .And.Contain("409").And.Contain(nameof(ConflictException)).And.Contain("traceId=");
        logs.Entries.Should().NotContain(e => e.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_WhenValidationException_LogsOneWarningWithoutException()
    {
        var logs = new CapturingLoggerProvider();
        RequestDelegate next = _ => throw new ValidationException(
            new[] { new FluentValidation.Results.ValidationFailure("Name", "Name is required") });
        using var host = await CreateHostAsync(isDevelopment: true, next, logs: logs);
        var client = host.GetTestClient();

        var response = await client.PostAsync("/bookings", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Warning);
        entries[0].Exception.Should().BeNull();
        entries[0].Message.Should().Contain("400").And.Contain(nameof(ValidationException));
    }

    [Fact]
    public async Task InvokeAsync_WhenUnhandledException_Returns500AndLogsOneErrorWithException()
    {
        var logs = new CapturingLoggerProvider();
        var thrown = new NullReferenceException("boom");
        RequestDelegate next = _ => throw thrown;
        using var host = await CreateHostAsync(isDevelopment: false, next, logs: logs);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/api/items");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Error);
        entries[0].Exception.Should().BeSameAs(thrown);
        entries[0].Message.Should().Contain("500").And.Contain(nameof(NullReferenceException));
    }

    [Fact]
    public async Task InvokeAsync_WhenMapperReturns5xx_LogsOneErrorWithException()
    {
        var logs = new CapturingLoggerProvider();
        var mapper = new TestExceptionMapper(503, new ProblemDetails { Title = "Service Unavailable" });
        RequestDelegate next = _ => throw new InvalidOperationException("storage down");
        using var host = await CreateHostAsync(isDevelopment: true, next, new[] { mapper }, logs);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Error);
        entries[0].Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Theory]
    [InlineData(true, "db")]
    [InlineData(false, "db")]
    [InlineData(true, "npgsql-io")]
    [InlineData(false, "npgsql-cast")]
    [InlineData(true, "npgsql-internal")]
    public async Task InvokeAsync_WhenProviderException_Returns500WithoutProviderText(bool isDevelopment, string kind)
    {
        const string leaked = "Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone' secret-db-text";
        RequestDelegate next = _ => throw CreateProviderException(kind, leaked);
        using var host = await CreateHostAsync(isDevelopment, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/catalog/tours");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("code").GetString().Should().Be("database_error");
        document.RootElement.GetProperty("detail").GetString().Should().Be("A database error occurred.");
        document.RootElement.GetProperty("title").GetString().Should().Be("Internal Server Error");
        AssertNoProviderText(document.RootElement);
    }

    [Fact]
    public async Task InvokeAsync_WhenArgumentExceptionMentionsProvider_KeepsClientMessageAndSkipsDatabaseCode()
    {
        const string leaked = "Cannot write DateTime with Kind=Unspecified to PostgreSQL type 'timestamp with time zone' secret-db-text";
        RequestDelegate next = _ => throw new ArgumentException(leaked);
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/catalog/tours");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("detail").GetString().Should().Be(leaked);
        document.RootElement.TryGetProperty("code", out var code).Should().BeFalse();
        code.ValueKind.Should().Be(JsonValueKind.Undefined);
    }

    [Theory]
    [InlineData("date_range", "date_range")]
    [InlineData("GreaterThanOrEqualValidator", "validation")]
    public async Task InvokeAsync_WhenValidationException_ExposesOnlyOwnedErrorCode(string errorCode, string expected)
    {
        var failure = new FluentValidation.Results.ValidationFailure("DateTo", "date is wrong")
        {
            ErrorCode = errorCode
        };
        RequestDelegate next = _ => throw new ValidationException(new[] { failure });
        using var host = await CreateHostAsync(isDevelopment: false, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/catalog/tours");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("code").GetString().Should().Be(expected);
    }

    [Fact]
    public async Task InvokeAsync_WhenOwnedAndBuiltinValidationCodesMix_ReturnsValidation()
    {
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("DateTo", "range") { ErrorCode = "date_range" },
            new FluentValidation.Results.ValidationFailure("Page", "page") { ErrorCode = "GreaterThanOrEqualValidator" }
        };
        RequestDelegate next = _ => throw new ValidationException(failures);
        using var host = await CreateHostAsync(isDevelopment: false, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/catalog/tours");
        var json = await response.Content.ReadAsStringAsync();

        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("code").GetString().Should().Be("validation");
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestAbortedWithOperationCanceled_LogsInformationWithoutBodyOrStack()
    {
        const string secret = "secret-db-text Npgsql Kind=Unspecified timestamp with time zone";
        var logs = new CapturingLoggerProvider();
        RequestDelegate next = context =>
        {
            context.Features.Set<IHttpRequestLifetimeFeature>(new CancelledRequestFeature());
            throw new OperationCanceledException(secret, new FakeDbException(secret));
        };
        using var host = await CreateHostAsync(isDevelopment: true, next, logs: logs);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/catalog/tours");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().NotBe(HttpStatusCode.InternalServerError);
        json.Should().NotContain("database_error");
        json.Should().NotContain(secret);
        json.Should().NotContain("Internal Server Error");

        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Information);
        entries[0].Exception.Should().BeNull();
        entries[0].Message.Should().Contain(nameof(OperationCanceledException));
        entries[0].Message.Should().NotContain(secret);
        entries[0].Message.Should().NotContain("at ");
        logs.Entries.Should().NotContain(entry => entry.Level == LogLevel.Error);
    }

    [Fact]
    public async Task InvokeAsync_WhenOperationCanceledWithoutAbort_LogsError()
    {
        var logs = new CapturingLoggerProvider();
        RequestDelegate next = _ => throw new OperationCanceledException("slow query");
        using var host = await CreateHostAsync(isDevelopment: false, next, logs: logs);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/catalog/tours");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var entries = MiddlewareEntries(logs);
        entries.Should().ContainSingle();
        entries[0].Level.Should().Be(LogLevel.Error);
        entries[0].Exception.Should().BeOfType<OperationCanceledException>();
    }

    private static Exception CreateProviderException(string kind, string leaked) => kind switch
    {
        "db" => new FakeDbException(leaked),
        "npgsql-io" => new Npgsql.InvalidOperationException(leaked),
        "npgsql-cast" => new Npgsql.InvalidCastException(leaked),
        "npgsql-internal" => new Npgsql.Internal.NpgsqlOperationException(leaked),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    [Fact]
    public async Task InvokeAsync_WhenDatabaseExceptionInDevelopment_DoesNotLeakProviderText()
    {
        const string leaked = "Npgsql.PostgresException secret-db-text: timestamp with time zone";
        RequestDelegate next = _ => throw new InvalidOperationException(
            "See the inner exception",
            new FakeDbException(leaked));
        using var host = await CreateHostAsync(isDevelopment: true, next);
        var client = host.GetTestClient();

        var response = await client.GetAsync("/catalog/tours");
        var json = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("code").GetString().Should().Be("database_error");
        document.RootElement.GetProperty("detail").GetString().Should().Be("A database error occurred.");
        AssertNoProviderText(document.RootElement);
    }

    private static void AssertNoProviderText(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    AssertNoProviderText(property.Value);
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    AssertNoProviderText(item);
                break;
            case JsonValueKind.String:
                var text = element.GetString() ?? string.Empty;
                text.Should().NotContain("Kind=Unspecified");
                text.Should().NotContain("PostgreSQL");
                text.Should().NotContain("Npgsql");
                text.Should().NotContain("secret-db-text");
                text.Should().NotContain("timestamp with time zone");
                break;
        }
    }

    private sealed class FakeDbException(string message) : DbException(message);

    private sealed class CancelledRequestFeature : IHttpRequestLifetimeFeature
    {
        private readonly CancellationTokenSource _cancellation = new();

        public CancelledRequestFeature()
        {
            _cancellation.Cancel();
            RequestAborted = _cancellation.Token;
        }

        public CancellationToken RequestAborted { get; set; }

        public void Abort() => _cancellation.Cancel();
    }

    private sealed class UnauthorizedException(string message) : Exception(message);

    private sealed class ClientErrorException(string message) : Exception(message);

    private sealed class StatusExceptionMapper(int statusCode, string title) : IExceptionMapper
    {
        public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
        {
            if (exception is UnauthorizedException or ClientErrorException)
            {
                result = (statusCode, new ProblemDetails
                {
                    Title = title,
                    Status = statusCode,
                    Detail = exception.Message,
                    Instance = context.Request.Path
                });
                return true;
            }

            result = default;
            return false;
        }
    }

    private sealed class ConflictException(string message) : Exception(message);

    private sealed class ConflictExceptionMapper : IExceptionMapper
    {
        public bool TryMap(Exception exception, HttpContext context, out (int StatusCode, ProblemDetails Details) result)
        {
            if (exception is ConflictException conflict)
            {
                result = (StatusCodes.Status409Conflict, new ProblemDetails { Title = "Conflict", Detail = conflict.Message });
                return true;
            }

            result = default;
            return false;
        }
    }
}
