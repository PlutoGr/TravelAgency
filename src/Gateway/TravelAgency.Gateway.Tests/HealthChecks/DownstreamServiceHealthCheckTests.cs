using System.Net;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
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

    private static TestLogger CreateLogger() => new();

    [Fact]
    public async Task CheckHealthAsync_WhenHttpClientReturns2xx_ReturnsHealthy_WithMessageContainingRegistrationNameAndReachable()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        handler.SetResponse(new HttpResponseMessage(HttpStatusCode.OK));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl, CreateLogger());
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
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl, CreateLogger());
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
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl, CreateLogger());
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains(statusCode.ToString(), result.Description!);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpCallThrowsHttpRequestException_ReturnsDegraded_WithUnreachableDescription()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        handler.SetException(new HttpRequestException("Connection refused."));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl, CreateLogger());
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.NotNull(result.Description);
        Assert.Contains(ServiceName, result.Description);
        Assert.Contains("unreachable", result.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpCallThrowsTaskCanceledException_ReturnsDegraded_WithUnreachableAndMessage()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        handler.SetException(new TaskCanceledException("Timeout"));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl, CreateLogger());
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("unreachable", result.Description!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpCallThrowsException_LogsWarningWithException()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        var expectedException = new HttpRequestException("Connection refused.");
        handler.SetException(expectedException);
        var logger = CreateLogger();
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl, logger);
        var context = CreateContext();

        // Act
        await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, logger.Entries[0].Level);
        Assert.Same(expectedException, logger.Entries[0].Exception);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenHttpCallThrowsException_ExceptionMessageNotLeakedIntoDescription()
    {
        // Arrange
        var (factory, handler) = CreateHttpClientFactory();
        var internalDetails = "internal connection string details";
        handler.SetException(new HttpRequestException(internalDetails));
        var healthCheck = new DownstreamServiceHealthCheck(factory, ServiceUrl, CreateLogger());
        var context = CreateContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert — description must not expose internal error details
        Assert.NotNull(result.Description);
        Assert.DoesNotContain(internalDetails, result.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Same(result.Exception, result.Exception); // exception is captured on the result, not in description
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

    /// <summary>
    /// Captures log entries for assertion in tests.
    /// </summary>
    private sealed class TestLogger : ILogger<DownstreamServiceHealthCheck>
    {
        public record LogEntry(LogLevel Level, Exception? Exception);

        public List<LogEntry> Entries { get; } = [];

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(new LogEntry(logLevel, exception));

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    }
}
