using MediatR;
using Microsoft.Extensions.Logging;
using TravelAgency.Media.Application.Behaviors;

namespace TravelAgency.Media.UnitTests.Application.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<TestRequest, TestResponse>> _logger =
        Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();

    private readonly LoggingBehavior<TestRequest, TestResponse> _behavior;

    public LoggingBehaviorTests()
    {
        _behavior = new LoggingBehavior<TestRequest, TestResponse>(_logger);
    }

    [Fact]
    public async Task Handle_OnSuccess_ReturnsResponseFromNext()
    {
        var expected = new TestResponse("result");
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expected);

        var result = await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_OnSuccess_LogsInformationTwice()
    {
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("ok"));

        await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        _logger.Received(2).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_LogsRequestNameOnStart()
    {
        var loggedMessages = new List<string>();
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("ok"));

        _logger.When(x => x.Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                Arg.Any<Exception?>(),
                Arg.Any<Func<object, Exception?, string>>()))
            .Do(ci =>
            {
                // Capture the formatted state string to verify request name is logged
                var state = ci.ArgAt<object>(2);
                if (state != null)
                    loggedMessages.Add(state.ToString() ?? string.Empty);
            });

        await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        loggedMessages.Should().Contain(m => m.Contains("TestRequest"));
    }

    [Fact]
    public async Task Handle_WhenNextThrows_RethrowsException()
    {
        var expectedException = new InvalidOperationException("test error");
        RequestHandlerDelegate<TestResponse> next = _ => throw expectedException;

        var act = async () => await _behavior.Handle(new TestRequest(), next, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("test error");
    }

    public record TestRequest : IRequest<TestResponse>;
    public record TestResponse(string Value);
}
