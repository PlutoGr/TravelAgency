using Microsoft.AspNetCore.Http;
using Serilog.Events;
using TravelAgency.Shared.Infrastructure.Logging;

namespace TravelAgency.Shared.Infrastructure.Tests.Logging;

public class RequestLoggingExtensionsTests
{
    private static HttpContext CreateContext(string path, int statusCode)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Response.StatusCode = statusCode;
        return context;
    }

    [Theory]
    [InlineData("/health/live", 200, LogEventLevel.Debug)]
    [InlineData("/health/ready", 200, LogEventLevel.Debug)]
    [InlineData("/health", 200, LogEventLevel.Debug)]
    [InlineData("/healthz", 200, LogEventLevel.Debug)]
    [InlineData("/HEALTH/LIVE", 200, LogEventLevel.Debug)]
    [InlineData("/health/live", 404, LogEventLevel.Debug)]
    [InlineData("/health/live", 503, LogEventLevel.Error)]
    [InlineData("/healthz", 500, LogEventLevel.Error)]
    [InlineData("/bookings", 201, LogEventLevel.Information)]
    [InlineData("/api/v1/catalog/tours", 200, LogEventLevel.Information)]
    [InlineData("/api/v1/catalog/tours/1", 404, LogEventLevel.Information)]
    [InlineData("/api/v1/catalog/tours/1", 428, LogEventLevel.Information)]
    [InlineData("/api/v1/catalog/tours", 500, LogEventLevel.Error)]
    [InlineData("/healthcheck-not-a-segment", 200, LogEventLevel.Information)]
    public void GetLevel_ByPathAndStatus_ReturnsExpectedLevel(string path, int statusCode, LogEventLevel expected)
    {
        var level = RequestLoggingExtensions.GetLevel(CreateContext(path, statusCode), 1.0, null);

        level.Should().Be(expected);
    }

    [Theory]
    [InlineData("/health/live", 200)]
    [InlineData("/bookings", 201)]
    public void GetLevel_WhenException_ReturnsError(string path, int statusCode)
    {
        var level = RequestLoggingExtensions.GetLevel(
            CreateContext(path, statusCode), 1.0, new InvalidOperationException("boom"));

        level.Should().Be(LogEventLevel.Error);
    }

    [Fact]
    public void GetLevel_PureOverload_MatchesHttpContextOverload()
    {
        RequestLoggingExtensions.GetLevel(new PathString("/health/live"), 200, null).Should().Be(LogEventLevel.Debug);
        RequestLoggingExtensions.GetLevel(new PathString("/health/live"), 503, null).Should().Be(LogEventLevel.Error);
    }
}
