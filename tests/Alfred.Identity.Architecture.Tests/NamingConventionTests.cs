using Alfred.Identity.Application;
using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.EmailTemplates;
using Alfred.Identity.Infrastructure;
using Alfred.Identity.WebApi.Configuration;

using NetArchTest.Rules;

namespace Alfred.Identity.Architecture.Tests;

/// <summary>
/// Tests to ensure naming conventions are followed across the solution
/// </summary>
public class NamingConventionTests
{
    [Fact]
    public void Interfaces_Should_StartWith_I()
    {
        // Arrange
        var domainAssembly = typeof(BaseEntity<>).Assembly;
        var applicationAssembly = typeof(ApplicationModule).Assembly;
        var infrastructureAssembly = typeof(InfrastructureModule).Assembly;

        // Act
        var result = Types.InAssemblies(new[] { domainAssembly, applicationAssembly, infrastructureAssembly })
            .That()
            .AreInterfaces()
            .Should()
            .HaveNameStartingWith("I")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All interfaces should start with 'I'. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Controllers_Should_EndWith_Controller()
    {
        // Arrange
        var assembly = typeof(AppConfiguration).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespaceEndingWith("Controllers")
            .And()
            .AreClasses()
            .Should()
            .HaveNameEndingWith("Controller")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"All controllers should end with 'Controller'. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Abstract_Classes_Should_StartWith_Base_Or_Abstract()
    {
        // Arrange
        var infrastructureAssembly = typeof(InfrastructureModule).Assembly;

        // Act
        var result = Types.InAssembly(infrastructureAssembly)
            .That()
            .AreAbstract()
            .And()
            .AreClasses()
            .And()
            .DoNotHaveName("InfrastructureModule") // Static class
            .And()
            .DoNotHaveNameEndingWith("Extensions") // Extension classes
            .And()
            .DoNotHaveNameEndingWith("Discovery") // Discovery classes
            .And()
            .DoNotHaveNameEndingWith("Helper") // Helper classes
            .Should()
            .HaveNameStartingWith("Base")
            .Or()
            .HaveNameStartingWith("Abstract")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Abstract classes should start with 'Base' or 'Abstract'. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }

    [Fact]
    public void Exception_Classes_Should_EndWith_Exception()
    {
        // Arrange
        var domainAssembly = typeof(EmailTemplate).Assembly;
        var applicationAssembly = typeof(ApplicationModule).Assembly;

        // Act
        var result = Types.InAssemblies(new[] { domainAssembly, applicationAssembly })
            .That()
            .Inherit(typeof(Exception))
            .Should()
            .HaveNameEndingWith("Exception")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful,
            $"Exception classes should end with 'Exception'. Violations: {string.Join(", ", result.FailingTypeNames ?? Array.Empty<string>())}");
    }
}
