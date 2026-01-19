using Alfred.Identity.Application;

using NetArchTest.Rules;

namespace Alfred.Identity.Architecture.Tests;

/// <summary>
/// Tests to ensure Application layer follows Clean Architecture principles
/// Application should only depend on Domain, not on Infrastructure or WebApi
/// </summary>
public class ApplicationLayerTests
{
    private const string DomainNamespace = "Alfred.Identity.Domain";
    private const string ApplicationNamespace = "Alfred.Identity.Application";
    private const string InfrastructureNamespace = "Alfred.Identity.Infrastructure";
    private const string WebApiNamespace = "Alfred.Identity.WebApi";

    [Fact]
    public void Application_Should_Not_HaveDependencyOn_Infrastructure()
    {
        // Arrange
        var assembly = typeof(ApplicationModule).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOn(InfrastructureNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application layer should not depend on Infrastructure layer. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOn_WebApi()
    {
        // Arrange
        var assembly = typeof(ApplicationModule).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application layer should not depend on WebApi layer. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Application_Can_DependOn_Domain()
    {
        // Arrange
        var assembly = typeof(ApplicationModule).Assembly;

        // Act - Count types that depend on Domain
        IEnumerable<Type>? typesWithDomainDependency = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(ApplicationNamespace)
            .And()
            .HaveDependencyOn(DomainNamespace)
            .GetTypes();

        // Assert - Application should have at least some types depending on Domain
        Assert.True(typesWithDomainDependency.Any(),
            "Application layer should have types that depend on Domain layer");
    }

    [Fact]
    public void Application_Services_Should_Have_ServiceSuffix()
    {
        // Arrange
        var assembly = typeof(ApplicationModule).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceEndingWith("Services")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .Should()
            .HaveNameEndingWith("Service")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Application services should end with 'Service' suffix. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
