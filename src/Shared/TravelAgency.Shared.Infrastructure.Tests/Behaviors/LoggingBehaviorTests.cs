using MediatR;
using Microsoft.Extensions.Logging;
using TravelAgency.Shared.Infrastructure.Behaviors;

namespace TravelAgency.Shared.Infrastructure.Tests.Behaviors;

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
    public async Task Handle_OnSuccess_CallsNext()
    {
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse("ok"));
        };

        await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        nextCalled.Should().BeTrue();
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
    public async Task Handle_WhenNextThrows_DoesNotLogException()
    {
        // The failed request is logged once by GlobalExceptionHandlerMiddleware (issue #57).
        RequestHandlerDelegate<TestResponse> next = _ => throw new InvalidOperationException("boom");

        try
        {
            await _behavior.Handle(new TestRequest(), next, CancellationToken.None);
        }
        catch { /* expected */ }

        _loggerMock.Verify(
            x => x.Log(
                It.Is<LogLevel>(level => level >= LogLevel.Warning),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        _loggerMock.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsNotNull<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    public record TestRequest : IRequest<TestResponse>;
    public record TestResponse(string Value);
}
