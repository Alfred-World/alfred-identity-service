using Alfred.Identity.Domain.Common.Base;

using FluentAssertions;

namespace Alfred.Identity.Domain.Tests.Common.Base;

// Concrete implementation for testing ValueObject
public class TestValueObject : ValueObject
{
    public string Property1 { get; }
    public int Property2 { get; }
    public string? Property3 { get; }

    public TestValueObject(string property1, int property2, string? property3 = null)
    {
        Property1 = property1;
        Property2 = property2;
        Property3 = property3;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Property1;
        yield return Property2;
        yield return Property3;
    }
}

// Another value object for testing different types
public class DifferentTestValueObject : ValueObject
{
    public string Property1 { get; }

    public DifferentTestValueObject(string property1)
    {
        Property1 = property1;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Property1;
    }
}

public class ValueObjectTests
{
    [Fact]
    public void Equals_WithSameValues_ShouldReturnTrue()
    {
        // Arrange
        TestValueObject vo1 = new("test", 123, "optional");
        TestValueObject vo2 = new("test", 123, "optional");

        // Act & Assert
        vo1.Should().Be(vo2);
        vo1.Equals(vo2).Should().BeTrue();
        vo1.Equals((object)vo2).Should().BeTrue();
        (vo1 == vo2).Should().BeTrue();
        (vo1 != vo2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentValues_ShouldReturnFalse()
    {
        // Arrange
        TestValueObject vo1 = new("test", 123, "optional");
        TestValueObject vo2 = new("different", 123, "optional");

        // Act & Assert
        vo1.Should().NotBe(vo2);
        vo1.Equals(vo2).Should().BeFalse();
        vo1.Equals((object)vo2).Should().BeFalse();
        (vo1 == vo2).Should().BeFalse();
        (vo1 != vo2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNullValues_ShouldHandleCorrectly()
    {
        // Arrange
        TestValueObject vo1 = new("test", 123, null);
        TestValueObject vo2 = new("test", 123, null);

        // Act & Assert
        vo1.Should().Be(vo2);
        vo1.Equals(vo2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithOneNullValue_ShouldReturnFalse()
    {
        // Arrange
        TestValueObject vo1 = new("test", 123, "value");
        TestValueObject vo2 = new("test", 123, null);

        // Act & Assert
        vo1.Should().NotBe(vo2);
        vo1.Equals(vo2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        // Arrange
        TestValueObject? vo = new("test", 123);

        // Act & Assert
        vo.Equals(null).Should().BeFalse();
        vo?.Equals((object?)null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        TestValueObject vo1 = new("test", 123);
        DifferentTestValueObject vo2 = new("test");

        // Act & Assert
        vo1.Equals(vo2).Should().BeFalse();
        vo1.Equals((object)vo2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        TestValueObject vo = new("test", 123);

        // Act & Assert
        vo.Should().Be(vo);
        vo.Equals(vo).Should().BeTrue();
        vo.Equals((object)vo).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldReturnSameHashCode()
    {
        // Arrange
        TestValueObject vo1 = new("test", 123, "optional");
        TestValueObject vo2 = new("test", 123, "optional");

        // Act & Assert
        vo1.GetHashCode().Should().Be(vo2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldReturnDifferentHashCode()
    {
        // Arrange
        TestValueObject vo1 = new("test", 123);
        TestValueObject vo2 = new("different", 123);

        // Act & Assert
        vo1.GetHashCode().Should().NotBe(vo2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithNullValues_ShouldNotThrow()
    {
        // Arrange
        TestValueObject vo = new("test", 123, null);

        // Act & Assert
        var hashCode = vo.GetHashCode();
        hashCode.Should().NotBe(0); // Just ensure it returns some value
    }

    [Fact]
    public void OperatorEquals_WithNulls_ShouldHandleCorrectly()
    {
        // Arrange
        TestValueObject? vo1 = null;
        TestValueObject? vo2 = null;
        TestValueObject vo3 = new("test", 123);

        // Act & Assert
        (vo1 == vo2).Should().BeTrue(); // both null
        (vo1 == vo3).Should().BeFalse(); // one null
        (vo3 == vo1).Should().BeFalse(); // one null
        (vo1 != vo2).Should().BeFalse(); // both null
        (vo1 != vo3).Should().BeTrue(); // one null
    }

    [Fact]
    public void ValueObject_WithComplexEqualityComponents_ShouldWorkCorrectly()
    {
        // Arrange
        TestValueObject vo1 = new("test", 0, null);
        TestValueObject vo2 = new("test", 0, null);
        TestValueObject vo3 = new("test", 1, null);

        // Act & Assert
        vo1.Should().Be(vo2);
        vo1.Should().NotBe(vo3);
        vo1.GetHashCode().Should().Be(vo2.GetHashCode());
        vo1.GetHashCode().Should().NotBe(vo3.GetHashCode());
    }
}
