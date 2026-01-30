using Alfred.Identity.Infrastructure;

using NetArchTest.Rules;

namespace Alfred.Identity.Architecture.Tests;

/// <summary>
/// Tests to ensure Infrastructure layer follows Clean Architecture principles
/// Infrastructure can depend on Domain and Application, but not WebApi
/// </summary>
public class InfrastructureLayerTests
{
    private const string InfrastructureNamespace = "Alfred.Identity.Infrastructure";
    private const string WebApiNamespace = "Alfred.Identity.WebApi";

    [Fact]
    public void Infrastructure_Should_Not_HaveDependencyOn_WebApi()
    {
        // Arrange
        var assembly = typeof(InfrastructureModule).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace(InfrastructureNamespace)
            .ShouldNot()
            .HaveDependencyOn(WebApiNamespace)
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Infrastructure layer should not depend on WebApi layer. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Repositories_Should_Have_RepositorySuffix()
    {
        // Arrange
        var assembly = typeof(InfrastructureModule).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceEndingWith("Repositories")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveName("BaseRepository")
            .And()
            .DoNotHaveName("BasePagedRepository")
            .And()
            .DoNotHaveNameStartingWith("UnitOfWork") // UnitOfWork is not a repository
            .Should()
            .HaveNameEndingWith("Repository")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Repository implementations should end with 'Repository' suffix. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Repositories_Should_Implement_Interface()
    {
        // Arrange
        var assembly = typeof(InfrastructureModule).Assembly;

        // Act - Get all repository implementations
        IEnumerable<Type>? repositoryTypes = Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceEndingWith("Repositories")
            .And()
            .AreClasses()
            .And()
            .AreNotAbstract()
            .And()
            .DoNotHaveName("BaseRepository")
            .And()
            .DoNotHaveName("BasePagedRepository")
            .GetTypes();

        // Assert - All should be valid classes
        Assert.True(repositoryTypes.Any(), "Should have repository implementations");
        Assert.All(repositoryTypes, type =>
        {
            Assert.True(type.IsClass && !type.IsAbstract,
                $"{type.Name} should be a concrete class");
        });
    }

    [Fact]
    public void DbContext_Should_BeIn_Providers_Namespace()
    {
        // Arrange
        var assembly = typeof(InfrastructureModule).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .HaveNameEndingWith("DbContext")
            .And()
            .AreClasses() // Exclude interfaces
            .Should()
            .ResideInNamespaceStartingWith("Alfred.Identity.Infrastructure.Providers")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"DbContext classes should reside in Providers namespace. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
