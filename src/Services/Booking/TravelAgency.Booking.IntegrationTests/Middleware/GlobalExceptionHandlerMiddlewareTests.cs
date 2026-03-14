using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Booking.API.Middleware;
using TravelAgency.Booking.Application.Exceptions;
using TravelAgency.Booking.Domain.Exceptions;

namespace TravelAgency.Booking.IntegrationTests.Middleware;

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
    public async Task WhenNotFoundExceptionThrown_Returns404WithProblemDetails()
    {
        var client = await CreateClientThatThrowsAsync(new NotFoundException("Booking not found"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Not Found");
        content.Should().Contain("Booking not found");
    }

    [Fact]
    public async Task WhenForbiddenExceptionThrown_Returns403()
    {
        var client = await CreateClientThatThrowsAsync(new ForbiddenException("Access denied"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Forbidden");
    }

    [Fact]
    public async Task WhenConflictExceptionThrown_Returns409()
    {
        var client = await CreateClientThatThrowsAsync(new ConflictException("Already exists"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Conflict");
    }

    [Fact]
    public async Task WhenBookingDomainExceptionThrown_Returns422()
    {
        var client = await CreateClientThatThrowsAsync(
            new BookingDomainException("Cannot transition from 'New' to 'Confirmed'."));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Unprocessable Entity");
        content.Should().Contain("Cannot transition");
    }

    [Fact]
    public async Task WhenUnhandledExceptionThrown_Returns500()
    {
        var client = await CreateClientThatThrowsAsync(new InvalidOperationException("Something went wrong"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task WhenUnhandledExceptionThrown_InDevelopment_IncludesExceptionMessage()
    {
        var client = await CreateClientThatThrowsAsync(
            new InvalidOperationException("Detailed internal error"), "Development");

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().Contain("Detailed internal error");
    }

    [Fact]
    public async Task WhenUnhandledExceptionThrown_InProduction_DoesNotExposeExceptionMessage()
    {
        var client = await CreateClientThatThrowsAsync(
            new InvalidOperationException("Sensitive DB connection string"), "Production");

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        content.Should().NotContain("Sensitive DB connection string");
        content.Should().Contain("unexpected error");
    }

    [Fact]
    public async Task Response_ContentType_IsApplicationProblemJson()
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
