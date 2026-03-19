using Alfred.Identity.Application.Common.Behaviors;

using FluentValidation;

namespace Alfred.Identity.Application.Tests.Common.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_ShouldInvokeNext()
    {
        // Arrange
        var behavior = new ValidationBehavior<FakeRequest, FakeResponse>(Array.Empty<IValidator<FakeRequest>>());
        var request = new FakeRequest("ok");
        var expected = new FakeResponse("passed");

        // Act
        var result = await behavior.Handle(request, _ => Task.FromResult(expected), CancellationToken.None);

        // Assert
        Assert.Equal(expected.Message, result.Message);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ShouldThrowValidationException()
    {
        // Arrange
        var validators = new IValidator<FakeRequest>[] { new FakeRequestValidator() };
        var behavior = new ValidationBehavior<FakeRequest, FakeResponse>(validators);
        var request = new FakeRequest(string.Empty);

        // Act
        var act = () => behavior.Handle(request, _ => Task.FromResult(new FakeResponse("should-not-run")), CancellationToken.None);

        // Assert
        await Assert.ThrowsAsync<ValidationException>(act);
    }

    [Fact]
    public async Task Handle_WhenValidationPasses_ShouldInvokeNext()
    {
        // Arrange
        var validators = new IValidator<FakeRequest>[] { new FakeRequestValidator() };
        var behavior = new ValidationBehavior<FakeRequest, FakeResponse>(validators);
        var request = new FakeRequest("valid");
        var expected = new FakeResponse("done");

        // Act
        var result = await behavior.Handle(request, _ => Task.FromResult(expected), CancellationToken.None);

        // Assert
        Assert.Equal(expected.Message, result.Message);
    }

    private sealed record FakeRequest(string Value);

    private sealed record FakeResponse(string Message);

    private sealed class FakeRequestValidator : AbstractValidator<FakeRequest>
    {
        public FakeRequestValidator()
        {
            RuleFor(x => x.Value).NotEmpty();
        }
    }
}
