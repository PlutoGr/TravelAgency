using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;
using TravelAgency.Media.API.Middleware;

namespace TravelAgency.Media.IntegrationTests.Middleware;

public class CorrelationIdTests
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    private static async Task<HttpClient> CreateClientWithMiddlewareAsync()
    {
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.Run(async ctx => await ctx.Response.WriteAsync("OK"));
                });
            })
            .StartAsync();
        return host.GetTestClient();
    }

    [Fact]
    public async Task Request_WithCorrelationIdHeader_ReturnsItInResponse()
    {
        var client = await CreateClientWithMiddlewareAsync();
        var existingId = Guid.NewGuid().ToString("D");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CorrelationIdHeader, existingId);
        var response = await client.SendAsync(request);

        response.Headers.Should().ContainKey(CorrelationIdHeader);
        var returnedId = response.Headers.GetValues(CorrelationIdHeader).First();
        returnedId.Should().Be(existingId);
    }

    [Fact]
    public async Task Request_WithoutCorrelationIdHeader_AddsOneInResponse()
    {
        var client = await CreateClientWithMiddlewareAsync();

        var response = await client.GetAsync("/");

        response.Headers.Should().ContainKey(CorrelationIdHeader);
        var correlationId = response.Headers.GetValues(CorrelationIdHeader).First();
        correlationId.Should().NotBeNullOrEmpty();
        Guid.TryParse(correlationId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Request_WithInvalidCorrelationId_GeneratesNewGuid()
    {
        var client = await CreateClientWithMiddlewareAsync();

        using var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add(CorrelationIdHeader, "invalid!@#$%^id");
        var response = await client.SendAsync(request);

        var responseId = response.Headers.GetValues(CorrelationIdHeader).First();
        responseId.Should().NotBe("invalid!@#$%^id");
        Guid.TryParse(responseId, out _).Should().BeTrue();
    }

    [Fact]
    public async Task TwoRequests_WithoutCorrelationId_GetDifferentIds()
    {
        var client = await CreateClientWithMiddlewareAsync();

        var response1 = await client.GetAsync("/");
        var response2 = await client.GetAsync("/");

        var id1 = response1.Headers.GetValues(CorrelationIdHeader).First();
        var id2 = response2.Headers.GetValues(CorrelationIdHeader).First();
        id1.Should().NotBe(id2);
    }
}
