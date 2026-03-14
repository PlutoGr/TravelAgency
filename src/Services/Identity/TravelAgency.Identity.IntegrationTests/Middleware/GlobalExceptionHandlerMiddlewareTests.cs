using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Identity.API.Middleware;
using TravelAgency.Identity.Application.Exceptions;

namespace TravelAgency.Identity.IntegrationTests.Middleware;

public class GlobalExceptionHandlerMiddlewareTests
{
    private static async Task<HttpClient> CreateClientThatThrowsAsync(
        Exception exceptionToThrow,
        string environment = "Development")
    {
        var ex = exceptionToThrow;
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.UseEnvironment(environment);
                webBuilder.ConfigureServices(services => services.AddLogging());
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
    public async Task WhenAppValidationExceptionThrown_Returns400WithErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Email"] = new[] { "Email is required" }
        } as IReadOnlyDictionary<string, string[]>;

        var client = await CreateClientThatThrowsAsync(new AppValidationException(errors!));

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
}
