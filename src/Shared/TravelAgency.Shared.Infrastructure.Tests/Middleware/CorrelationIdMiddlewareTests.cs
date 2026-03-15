using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Shared.Infrastructure.Middleware;

namespace TravelAgency.Shared.Infrastructure.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    private static async Task<IHost> CreateHostAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(_ => { })
                    .Configure(app =>
                    {
                        app.UseMiddleware<CorrelationIdMiddleware>();
                        app.Run(context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.OK;
                            return context.Response.WriteAsync(context.TraceIdentifier);
                        });
                    });
            })
            .StartAsync();
        return host;
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasNoCorrelationIdHeader_SetsXCorrelationIdInResponse()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");

        response.EnsureSuccessStatusCode();
        response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues).Should().BeTrue();
        var correlationId = headerValues!.Single();
        correlationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue("Generated value should be a valid GUID.");
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasNoCorrelationIdHeader_SetsTraceIdentifierAndEchoesInBody()
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();

        var response = await client.GetAsync("/");
        var body = await response.Content.ReadAsStringAsync();
        response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues).Should().BeTrue();
        var correlationId = headerValues!.Single();

        body.Should().Be(correlationId);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasValidCorrelationId_UsesSameValueForTraceIdentifierAndResponseHeader()
    {
        var validId = "abc123-XyZ-42";
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, validId);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues).Should().BeTrue();
        headerValues!.Single().Should().Be(validId);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(validId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvokeAsync_WhenHeaderIsEmptyOrWhitespace_GeneratesNewGuid(string headerValue)
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, headerValue);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues).Should().BeTrue();
        var correlationId = headerValues!.Single();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderLengthExceeds128_GeneratesNewGuid()
    {
        var tooLong = new string('a', 129);
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, tooLong);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues).Should().BeTrue();
        var correlationId = headerValues!.Single();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
        correlationId.Should().NotBe(tooLong);
    }

    [Theory]
    [InlineData("id_with_underscore")]
    [InlineData("id.with.dots")]
    [InlineData("id with spaces")]
    public async Task InvokeAsync_WhenHeaderContainsInvalidCharacters_GeneratesNewGuid(string invalidValue)
    {
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, invalidValue);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues).Should().BeTrue();
        var correlationId = headerValues!.Single();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderIsValidMaxLength128_UsesSameValue()
    {
        var valid128 = new string('a', 128);
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, valid128);

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();
        response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues).Should().BeTrue();
        headerValues!.Single().Should().Be(valid128);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be(valid128);
    }
}
