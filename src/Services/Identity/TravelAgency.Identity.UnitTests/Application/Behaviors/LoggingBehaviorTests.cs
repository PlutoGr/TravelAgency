using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using TravelAgency.Identity.Application.Behaviors;

namespace TravelAgency.Identity.UnitTests.Application.Behaviors;

public class LoggingBehaviorTests
{
    private readonly Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>> _loggerMock;
    private readonly LoggingBehavior<TestRequest, TestResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        _behavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);
    }

    [Fact]
    public async Task Handle_OnSuccess_ReturnsResponseFromNext()
    {
        var expectedResponse = new TestResponse("result");
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expectedResponse);

        var result = await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        result.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Handle_OnSuccess_LogsInformationTwice()
    {
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("ok"));

        await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenNextThrows_RethrowsException()
    {
        var expectedException = new InvalidOperationException("test error");
        RequestHandlerDelegate<TestResponse> next = _ => throw expectedException;

        var act = async () => await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("test error");
    }

    [Fact]
    public async Task Handle_WhenNextThrows_LogsError()
    {
        RequestHandlerDelegate<TestResponse> next = _ => throw new InvalidOperationException("boom");

        try
        {
            await _behavior.Handle(new TestRequest(), next, CancellationToken.None);
        }
        catch { /* expected */ }

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    public record TestRequest : IRequest<TestResponse>;
    public record TestResponse(string Value);
}
