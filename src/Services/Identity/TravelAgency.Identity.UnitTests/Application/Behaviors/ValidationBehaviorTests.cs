using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using TravelAgency.Identity.Application.Behaviors;
using TravelAgency.Identity.Application.Exceptions;

namespace TravelAgency.Identity.UnitTests.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_CallsNextAndReturnsResult()
    {
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IValidator<TestRequest>>());
        var expected = new TestResponse("ok");
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expected);

        var result = await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_CallsNextAndReturnsResult()
    {
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator.Object });
        var expected = new TestResponse("ok");
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expected);

        var result = await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ThrowsAppValidationException()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Password", "Password is too short")
        };
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator.Object });
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("unused"));

        var act = async () => await behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AppValidationException>();
        exception.Which.Errors.Should().ContainKey("Email");
        exception.Which.Errors.Should().ContainKey("Password");
        exception.Which.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_GroupsErrorsByPropertyName()
    {
        var failures = new List<ValidationFailure>
        {
            new("Email", "Email is required"),
            new("Email", "Email must be valid")
        };
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator.Object });

        var act = async () => await behavior.Handle(
            new TestRequest("invalid"),
            _ => Task.FromResult(new TestResponse("unused")),
            CancellationToken.None);

        var exception = await act.Should().ThrowAsync<AppValidationException>();
        exception.Which.Errors["Email"].Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_DoesNotCallNext()
    {
        var failures = new List<ValidationFailure> { new("Prop", "Error") };
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<TestRequest>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator.Object });
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse("unused"));
        };

        try { await behavior.Handle(new TestRequest("x"), next, CancellationToken.None); } catch { }

        nextCalled.Should().BeFalse();
    }

    public record TestRequest(string Value) : IRequest<TestResponse>;
    public record TestResponse(string Value);
}
