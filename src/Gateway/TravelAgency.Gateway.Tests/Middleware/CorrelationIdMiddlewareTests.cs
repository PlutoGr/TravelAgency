using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelAgency.Gateway.Middleware;

namespace TravelAgency.Gateway.Tests.Middleware;

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
                    .ConfigureServices(services => { })
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
    public async Task InvokeAsync_WhenRequestHasNoCorrelationIdHeader_GeneratesNewGuid_SetsTraceIdentifierAndResponseHeader_AndCallsNext()
    {
        // Arrange
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues));
        var correlationId = Assert.Single(headerValues);
        Assert.NotNull(correlationId);
        Assert.True(Guid.TryParse(correlationId, out _), "Generated value should be a valid GUID.");
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(correlationId, body);
    }

    [Fact]
    public async Task InvokeAsync_WhenRequestHasValidCorrelationId_UseSameValueForTraceIdentifierAndResponseHeader()
    {
        // Arrange
        var validId = "abc123-XyZ-42";
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, validId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues));
        Assert.Equal(validId, Assert.Single(headerValues));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(validId, body);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task InvokeAsync_WhenHeaderIsEmptyOrWhitespace_GeneratesNewGuid(string headerValue)
    {
        // Arrange
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, headerValue);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues));
        var correlationId = Assert.Single(headerValues);
        Assert.True(Guid.TryParse(correlationId, out _));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(correlationId, body);
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderLengthExceeds128_GeneratesNewGuid()
    {
        // Arrange
        var tooLong = new string('a', 129);
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, tooLong);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues));
        var correlationId = Assert.Single(headerValues);
        Assert.True(Guid.TryParse(correlationId, out _));
        Assert.NotEqual(tooLong, correlationId);
    }

    [Theory]
    [InlineData("id_with_underscore")]
    [InlineData("id.with.dots")]
    [InlineData("id with spaces")]
    [InlineData("id\u0000null")]
    public async Task InvokeAsync_WhenHeaderContainsInvalidCharacters_GeneratesNewGuid(string invalidValue)
    {
        // Arrange
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, invalidValue);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues));
        var correlationId = Assert.Single(headerValues);
        Assert.True(Guid.TryParse(correlationId, out _));
    }

    [Fact]
    public async Task InvokeAsync_WhenHeaderIsValidMaxLength128_UseSameValue()
    {
        // Arrange
        var valid128 = new string('a', 128);
        using var host = await CreateHostAsync();
        var client = host.GetTestClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.TryAddWithoutValidation(CorrelationIdHeader, valid128);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.TryGetValues(CorrelationIdHeader, out var headerValues));
        Assert.Equal(valid128, Assert.Single(headerValues));
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(valid128, body);
    }
}
