using Alfred.Identity.Domain.Common.Base;

using NetArchTest.Rules;

namespace Alfred.Identity.Architecture.Tests;

/// <summary>
/// Tests to ensure Domain layer follows Clean Architecture principles
/// Domain should not depend on any other layers
/// </summary>
public class DomainLayerTests
{
    private const string DomainNamespace = "Alfred.Identity.Domain";
    private const string ApplicationNamespace = "Alfred.Identity.Application";
    private const string InfrastructureNamespace = "Alfred.Identity.Infrastructure";
    private const string WebApiNamespace = "Alfred.Identity.WebApi";

    [Fact]
    public void Domain_Should_Not_HaveDependencyOn_Application()
    {
        // Arrange
        var assembly = typeof(BaseEntity<>).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(ApplicationNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not depend on Application layer. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_Should_Not_HaveDependencyOn_Infrastructure()
    {
        // Arrange
        var assembly = typeof(BaseEntity<>).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not depend on Infrastructure layer. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_Should_Not_HaveDependencyOn_WebApi()
    {
        // Arrange
        var assembly = typeof(BaseEntity<>).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(DomainNamespace)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain layer should not depend on WebApi layer. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Domain_Entities_Should_BeSealed_Or_Abstract()
    {
        // Arrange
        var assembly = typeof(BaseEntity<>).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace($"{DomainNamespace}.*.Entities")
            .And()
            .AreClasses()
            .Should()
            .BeSealed()
            .Or()
            .BeAbstract()
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Domain entities should be sealed or abstract to prevent inheritance issues. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
