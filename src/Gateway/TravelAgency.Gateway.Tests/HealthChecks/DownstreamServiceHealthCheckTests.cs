using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using Xunit;
using TravelAgency.Gateway.HealthChecks;

namespace TravelAgency.Gateway.Tests.HealthChecks;

public class DownstreamServiceHealthCheckTests
{
    private const string ServiceName = "BookingService";
    private const string ServiceUrl = "https://booking-service/health";

    private static HealthCheckContext CreateContext()
    {
        var registration = new HealthCheckRegistration(
            ServiceName,
            _ => null!,
            failureStatus: null,
            tags: null);
        return new HealthCheckContext { Registration = registration };
    }

    private static (IHttpClientFactory Factory, MockHttpMessageHandler Handler) CreateHttpClientFactory()
    {
        var handler = new MockHttpMessageHandler();
        var client = new HttpClient(handler);
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("HealthChecks").Returns(client);
        return (factory, handler);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpClientReturns2xx_ReturnsHealthy_WithMessageContainingRegistrationNameAndReachable()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        handler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl);
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains(ServiceName, result.Description);
        Assert.Contains("reachable", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpClientReturnsNon2xx_ReturnsDegraded_WithMessageContainingStatusCode()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        handler.SetResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl);
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains(ServiceName, result.Description);
        Assert.Contains("NotFound", result.Description);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task CheckHealthAsync_WhenHttpClientReturnsNonSuccess_ReturnsDegraded_WithStatusCodeInMessage(HttpStatusCode statusCode)
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        handler.SetResponse(new HttpResponseMessage(statusCode));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl);
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains(statusCode.ToString(), result.Description!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpCallThrowsHttpRequestException_ReturnsDegraded_WithUnreachableAndExceptionMessage()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        var exceptionMessage = "Connection refused.";
        handler.SetException(new HttpRequestException(exceptionMessage));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl);
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains(ServiceName, result.Description);
        Assert.Contains("unreachable", result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(exceptionMessage, result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpCallThrowsTaskCanceledException_ReturnsDegraded_WithUnreachableAndMessage()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        handler.SetException(new TaskCanceledException("Timeout"));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl);
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("unreachable", result.Description!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Minimal handler to control HTTP response or exception for health check tests.
    /// </summary>
    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private HttpResponseMessage? _response;
        private Exception? _exception;

        public void SetResponse(HttpResponseMessage response) => _response = response;
        public void SetException(Exception ex) => _exception = ex;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception != null)
                return Task.FromException<HttpResponseMessage>(_exception);
            return Task.FromResult(_response ?? new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
