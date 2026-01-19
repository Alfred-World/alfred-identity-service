namespace Alfred.Identity.Infrastructure.Common.Seeding;

/// <summary>
/// Interface for seeders that should only run in specific environments
/// Implement this interface along with IDataSeeder to restrict seeder execution to certain environments
/// </summary>
public interface IEnvironmentAwareSeeder
{
    /// <summary>
    /// Get the environments where this seeder should run
    /// Examples: "Development", "Production", "Development,Staging"
    /// </summary>
    string[] AllowedEnvironments { get; }
}
