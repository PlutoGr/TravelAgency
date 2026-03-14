using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Media.API.Middleware;
using TravelAgency.Media.Application.Exceptions;
using TravelAgency.Media.Domain.Exceptions;

namespace TravelAgency.Media.IntegrationTests.Middleware;

public class GlobalExceptionHandlerTests
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
    public async Task Get_UnknownId_Returns404AsProblemDetails()
    {
        var mediaId = Guid.NewGuid();
        var client = await CreateClientThatThrowsAsync(new MediaNotFoundException(mediaId));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(404);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Not Found");
    }

    [Fact]
    public async Task WhenMediaAccessDeniedException_Returns403AsProblemDetails()
    {
        var mediaId = Guid.NewGuid();
        var client = await CreateClientThatThrowsAsync(new MediaAccessDeniedException(mediaId));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(403);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Forbidden");
    }

    [Fact]
    public async Task WhenValidationException_Returns400AsProblemDetails()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["ContentType"] = ["Content type must be one of: image/jpeg."]
        } as IReadOnlyDictionary<string, string[]>;

        var client = await CreateClientThatThrowsAsync(new ValidationException(errors!));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be(400);
        doc.RootElement.GetProperty("title").GetString().Should().Be("Validation Error");
    }

    [Fact]
    public async Task WhenUnhandledException_Returns500AsProblemDetails()
    {
        var client = await CreateClientThatThrowsAsync(new InvalidOperationException("Unexpected failure"));

        var response = await client.GetAsync("/");

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task Response_ContainsTraceId()
    {
        var client = await CreateClientThatThrowsAsync(new MediaNotFoundException(Guid.NewGuid()));

        var response = await client.GetAsync("/");
        var json = await response.Content.ReadAsStringAsync();

        json.Should().Contain("traceId");
    }

    [Fact]
    public async Task WhenUnhandledException_InDevelopment_IncludesExceptionMessage()
    {
        var client = await CreateClientThatThrowsAsync(
            new InvalidOperationException("Detailed internal message"), "Development");

        var json = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        json.Should().Contain("Detailed internal message");
    }

    [Fact]
    public async Task WhenUnhandledException_InProduction_DoesNotExposeExceptionMessage()
    {
        var client = await CreateClientThatThrowsAsync(
            new InvalidOperationException("Sensitive internal detail"), "Production");

        var json = await (await client.GetAsync("/")).Content.ReadAsStringAsync();

        json.Should().NotContain("Sensitive internal detail");
        json.Should().Contain("unexpected error");
    }
}
