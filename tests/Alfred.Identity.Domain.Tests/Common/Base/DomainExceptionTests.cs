using Alfred.Identity.Domain.Common.Base;

using FluentAssertions;

namespace Alfred.Identity.Domain.Tests.Common.Base;

public class DomainExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_ShouldCreateException()
    {
        // Arrange
        const string message = "This is a domain exception";

        // Act
        DomainException exception = new(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
        exception.Details.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldCreateException()
    {
        // Arrange
        const string message = "This is a domain exception";
        InvalidOperationException innerException = new("Inner exception");

        // Act
        DomainException exception = new(message, innerException);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.Details.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithMessageAndDetails_ShouldCreateException()
    {
        // Arrange
        const string message = "This is a domain exception";
        Dictionary<string, object> details = new()
        {
            { "Field", "Name" },
            { "Value", "Invalid Value" },
            { "Code", 12345 }
        };

        // Act
        DomainException exception = new(message, details);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeNull();
        exception.Details.Should().NotBeNull();
        exception.Details.Should().BeEquivalentTo(details);
    }

    [Fact]
    public void Constructor_WithEmptyMessage_ShouldCreateException()
    {
        // Arrange
        const string message = "";

        // Act
        DomainException exception = new(message);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Constructor_WithEmptyDetails_ShouldCreateException()
    {
        // Arrange
        const string message = "Domain exception";
        Dictionary<string, object> emptyDetails = new();

        // Act
        DomainException exception = new(message, emptyDetails);

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be(message);
        exception.Details.Should().NotBeNull();
        exception.Details.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Field cannot be empty")]
    [InlineData("Value must be positive")]
    [InlineData("Code already exists")]
    [InlineData("Maximum length exceeded")]
    public void Constructor_WithDifferentMessages_ShouldCreateException(string message)
    {
        // Act
        DomainException exception = new(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void Details_WithDifferentValueTypes_ShouldStoreCorrectly()
    {
        // Arrange
        const string message = "Domain exception";
        Dictionary<string, object> details = new()
        {
            { "StringValue", "test" },
            { "IntValue", 42 },
            { "BoolValue", true },
            { "DateValue", DateTime.Now },
            { "NullValue", null! }
        };

        // Act
        DomainException exception = new(message, details);

        // Assert
        exception.Details.Should().NotBeNull();
        exception.Details!["StringValue"].Should().Be("test");
        exception.Details["IntValue"].Should().Be(42);
        exception.Details["BoolValue"].Should().Be(true);
        exception.Details["DateValue"].Should().BeOfType<DateTime>();
        exception.Details["NullValue"].Should().BeNull();
    }

    [Fact]
    public void InheritanceFromException_ShouldWorkCorrectly()
    {
        // Arrange
        const string message = "Domain exception";
        DomainException exception = new(message);

        // Act & Assert
        exception.Should().BeAssignableTo<Exception>();
        exception.Should().BeOfType<DomainException>();
    }

    [Fact]
    public void Throw_ShouldBeCatchableAsException()
    {
        // Arrange
        const string message = "Domain exception";

        // Act & Assert
        Action act = () => throw new DomainException(message);

        act.Should().Throw<DomainException>()
            .WithMessage(message);

        act.Should().Throw<Exception>()
            .WithMessage(message);
    }

    [Fact]
    public void Throw_WithDetails_ShouldPreserveDetails()
    {
        // Arrange
        const string message = "Domain exception";
        Dictionary<string, object> details = new() { { "Field", "TestField" } };

        // Act & Assert
        Action act = () => throw new DomainException(message, details);

        act.Should().Throw<DomainException>()
            .Which.Details.Should().BeEquivalentTo(details);
    }
}
