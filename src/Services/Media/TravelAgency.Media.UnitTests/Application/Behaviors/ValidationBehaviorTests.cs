using FluentValidation;
using FluentValidation.Results;
using MediatR;
using TravelAgency.Media.Application.Behaviors;
using AppValidationException = TravelAgency.Media.Application.Exceptions.ValidationException;

namespace TravelAgency.Media.UnitTests.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_NoValidatorsRegistered_CallsNextAndReturnsResult()
    {
        var behavior = new ValidationBehavior<TestRequest, TestResponse>(
            Enumerable.Empty<IValidator<TestRequest>>());
        var expected = new TestResponse("ok");
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expected);

        var result = await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_ValidatorPassses_CallsNextAndReturnsResult()
    {
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator });
        var expected = new TestResponse("ok");
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expected);

        var result = await behavior.Handle(new TestRequest("valid"), next, CancellationToken.None);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task Handle_ValidatorFails_ThrowsValidationException()
    {
        var failures = new List<ValidationFailure>
        {
            new("FileName", "File name is required."),
            new("ContentType", "Content type is invalid.")
        };

        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator });
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse("unused"));

        var act = async () => await behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("FileName");
        ex.Which.Errors.Should().ContainKey("ContentType");
    }

    [Fact]
    public async Task Handle_ValidatorFails_GroupsMultipleErrorsForSameProperty()
    {
        var failures = new List<ValidationFailure>
        {
            new("FileName", "File name is required."),
            new("FileName", "File name must not contain spaces.")
        };

        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator });

        var act = async () => await behavior.Handle(
            new TestRequest("invalid"),
            _ => Task.FromResult(new TestResponse("unused")),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors["FileName"].Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ValidatorFails_DoesNotCallNext()
    {
        var failures = new List<ValidationFailure> { new("SomeProperty", "Some error.") };
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator });
        var nextCalled = false;
        RequestHandlerDelegate<TestResponse> next = _ =>
        {
            nextCalled = true;
            return Task.FromResult(new TestResponse("unused"));
        };

        try { await behavior.Handle(new TestRequest("invalid"), next, CancellationToken.None); } catch { }

        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MultipleValidatorsAllFail_AggregatesAllErrors()
    {
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("FileName", "Name error.") }));

        var validator2 = Substitute.For<IValidator<TestRequest>>();
        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { new ValidationFailure("ContentType", "Type error.") }));

        var behavior = new ValidationBehavior<TestRequest, TestResponse>(new[] { validator1, validator2 });

        var act = async () => await behavior.Handle(
            new TestRequest("invalid"),
            _ => Task.FromResult(new TestResponse("unused")),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<AppValidationException>();
        ex.Which.Errors.Should().ContainKey("FileName");
        ex.Which.Errors.Should().ContainKey("ContentType");
    }

    public record TestRequest(string Value) : IRequest<TestResponse>;
    public record TestResponse(string Value);
}
