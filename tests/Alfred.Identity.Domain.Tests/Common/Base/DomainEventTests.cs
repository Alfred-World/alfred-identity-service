using Alfred.Identity.Domain.Common.Base;

using FluentAssertions;

namespace Alfred.Identity.Domain.Tests.Common.Base;

// Concrete implementation for testing DomainEvent
public sealed record TestDomainEvent(string Message, int Value) : DomainEvent;

public class DomainEventTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        TestDomainEvent domainEvent = new("Test message", 42);

        // Assert
        domainEvent.Should().NotBeNull();
        domainEvent.Message.Should().Be("Test message");
        domainEvent.Value.Should().Be(42);
        domainEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.OccurredOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        domainEvent.EventId.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_MultipleInstances_ShouldHaveUniqueEventIds()
    {
        // Act
        TestDomainEvent event1 = new("Message 1", 1);
        TestDomainEvent event2 = new("Message 2", 2);

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    [Fact]
    public void Constructor_MultipleInstances_ShouldHaveCloseTimestamps()
    {
        // Act
        TestDomainEvent event1 = new("Message 1", 1);
        TestDomainEvent event2 = new("Message 2", 2);

        // Assert
        event1.OccurredAt.Should().BeCloseTo(event2.OccurredAt, TimeSpan.FromSeconds(1));
        event1.OccurredOn.Should().BeCloseTo(event2.OccurredOn, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void OccurredAt_And_OccurredOn_ShouldBeTheSame()
    {
        // Act
        TestDomainEvent domainEvent = new("Test", 1);

        // Assert
        domainEvent.OccurredAt.Should().BeCloseTo(domainEvent.OccurredOn, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void Equals_WithSameData_ShouldConsiderRecordEquality()
    {
        // Arrange
        TestDomainEvent event1 = new("Test message", 42);
        TestDomainEvent event2 = new("Test message", 42);

        // Act & Assert
        // Note: Even with same data, records will be different because of EventId and timestamps
        event1.Message.Should().Be(event2.Message);
        event1.Value.Should().Be(event2.Value);

        // But the EventId will be different
        event1.EventId.Should().NotBe(event2.EventId);

        // And timestamps might be slightly different
        event1.OccurredAt.Should().BeCloseTo(event2.OccurredAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EventId_ShouldBeValidGuid()
    {
        // Act
        TestDomainEvent domainEvent = new("Test", 1);

        // Assert
        domainEvent.EventId.Should().NotBe(Guid.Empty);
        Guid.TryParse(domainEvent.EventId.ToString(), out _).Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("Test message")]
    [InlineData("Very long test message with lots of text")]
    public void Constructor_WithDifferentMessages_ShouldCreateCorrectly(string message)
    {
        // Act
        TestDomainEvent domainEvent = new(message, 1);

        // Assert
        domainEvent.Message.Should().Be(message);
    }

    [Theory]
    [InlineData(int.MinValue)]
    [InlineData(0)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void Constructor_WithDifferentValues_ShouldCreateCorrectly(int value)
    {
        // Act
        TestDomainEvent domainEvent = new("Test", value);

        // Assert
        domainEvent.Value.Should().Be(value);
    }
}
