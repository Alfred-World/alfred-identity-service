using Alfred.Identity.Domain.Common.Base;

using FluentAssertions;

namespace Alfred.Identity.Domain.Tests.Common.Base;

// Concrete implementation for testing BaseEntity
public class TestEntity : BaseEntity
{
    public TestEntity() : base()
    {
    }

    public TestEntity(Guid id) : base(id)
    {
    }

    public void SetId(Guid id)
    {
        Id = id;
    }
}

// Concrete implementation for testing BaseEntity<T>
public class TestEntityWithStringId : BaseEntity<string>
{
    public TestEntityWithStringId() : base()
    {
    }

    public TestEntityWithStringId(string id) : base(id)
    {
    }

    public void SetId(string id)
    {
        Id = id;
    }
}

public class BaseEntityTests
{
    private static readonly Guid _testGuid1 = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef");
    private static readonly Guid _testGuid2 = Guid.Parse("fedcba98-7654-3210-fedc-ba9876543210");

    [Fact]
    public void Constructor_WithoutId_ShouldCreateEntityWithUuidV7()
    {
        // Act
        TestEntity entity = new();

        // Assert
        entity.Id.Should().NotBe(Guid.Empty); // UUID v7 should be generated
    }

    [Fact]
    public void Constructor_WithId_ShouldCreateEntityWithSpecifiedId()
    {
        // Arrange
        var expectedId = _testGuid1;

        // Act
        TestEntity entity = new(expectedId);

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    [Fact]
    public void Equals_WithSameIdAndType_ShouldReturnTrue()
    {
        // Arrange
        var id = _testGuid1;
        TestEntity entity1 = new();
        entity1.SetId(id);
        TestEntity entity2 = new();
        entity2.SetId(id);

        // Act & Assert
        entity1.Should().Be(entity2);
        entity1.Equals(entity2).Should().BeTrue();
        (entity1 == entity2).Should().BeTrue();
        (entity1 != entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentIds_ShouldReturnFalse()
    {
        // Arrange
        TestEntity entity1 = new();
        entity1.SetId(_testGuid1);
        TestEntity entity2 = new();
        entity2.SetId(_testGuid2);

        // Act & Assert
        entity1.Should().NotBe(entity2);
        entity1.Equals(entity2).Should().BeFalse();
        (entity1 == entity2).Should().BeFalse();
        (entity1 != entity2).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithTransientEntities_ShouldReturnFalse()
    {
        // Arrange
        TestEntity entity1 = new();
        entity1.SetId(Guid.Empty); // Transient
        TestEntity entity2 = new();
        entity2.SetId(Guid.Empty); // Transient

        // Act & Assert
        entity1.Should().NotBe(entity2);
        entity1.Equals(entity2).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        // Arrange
        TestEntity entity = new();
        entity.SetId(_testGuid1);

        // Act & Assert
        entity.Should().Be(entity);
        entity.Equals(entity).Should().BeTrue();
        ReferenceEquals(entity, entity).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithNullObject_ShouldReturnFalse()
    {
        // Arrange
        TestEntity? entity = new();
        entity.SetId(_testGuid1);

        // Act & Assert
        entity.Equals(null).Should().BeFalse();
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        // Arrange
        TestEntity entity = new();
        entity.SetId(_testGuid1);
        var differentObject = "not an entity";

        // Act & Assert
        entity.Equals(differentObject).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_WithSameIdAndType_ShouldReturnSameHashCode()
    {
        // Arrange
        var id = _testGuid1;
        TestEntity entity1 = new();
        entity1.SetId(id);
        TestEntity entity2 = new();
        entity2.SetId(id);

        // Act & Assert
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentIds_ShouldReturnDifferentHashCode()
    {
        // Arrange
        TestEntity entity1 = new();
        entity1.SetId(_testGuid1);
        TestEntity entity2 = new();
        entity2.SetId(_testGuid2);

        // Act & Assert
        entity1.GetHashCode().Should().NotBe(entity2.GetHashCode());
    }

    [Fact]
    public void OperatorEquals_WithNullOperands_ShouldHandleCorrectly()
    {
        // Arrange
        TestEntity? entity1 = null;
        TestEntity? entity2 = null;
        TestEntity entity3 = new();
        entity3.SetId(_testGuid1);

        // Act & Assert
        (entity1 == entity2).Should().BeTrue(); // both null
        (entity1 == entity3).Should().BeFalse(); // one null
        (entity3 == entity1).Should().BeFalse(); // one null
    }

    [Fact]
    public void OperatorNotEquals_ShouldBeOppositeOfEquals()
    {
        // Arrange
        TestEntity entity1 = new();
        entity1.SetId(_testGuid1);
        TestEntity entity2 = new();
        entity2.SetId(_testGuid1);
        TestEntity entity3 = new();
        entity3.SetId(_testGuid2);

        // Act & Assert
        (entity1 != entity2).Should().BeFalse(); // same
        (entity1 != entity3).Should().BeTrue(); // different
    }
}

public class BaseEntityGenericTests
{
    [Fact]
    public void Constructor_WithStringId_ShouldCreateEntity()
    {
        // Arrange
        const string expectedId = "TEST123";

        // Act
        TestEntityWithStringId entity = new(expectedId);

        // Assert
        entity.Id.Should().Be(expectedId);
    }

    [Fact]
    public void Equals_WithStringIds_ShouldWorkCorrectly()
    {
        // Arrange
        const string id = "TEST123";
        TestEntityWithStringId entity1 = new();
        entity1.SetId(id);
        TestEntityWithStringId entity2 = new();
        entity2.SetId(id);

        // Act & Assert
        entity1.Should().Be(entity2);
        entity1.GetHashCode().Should().Be(entity2.GetHashCode());
    }

    [Fact]
    public void Equals_WithEmptyStringId_ShouldBeTransient()
    {
        // Arrange
        TestEntityWithStringId entity1 = new(); // Id = null, transient
        TestEntityWithStringId entity2 = new(); // Id = null, transient

        // Act & Assert
        entity1.Should().NotBe(entity2); // transient entities should not be equal
    }
}
