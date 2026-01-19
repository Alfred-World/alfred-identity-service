namespace Alfred.Identity.Infrastructure.Common.Seeding;

/// <summary>
/// Interface for data seeders
/// Seeders are executed in alphabetical order by Name.
/// Use timestamp prefix in Name (e.g., "20251129140000_SigningKeySeeder") for ordering.
/// </summary>
public interface IDataSeeder
{
    /// <summary>
    /// Seed data asynchronously
    /// </summary>
    Task SeedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the name of this seeder (used for ordering and tracking).
    /// Should be in format: "{timestamp}_{SeederName}" (e.g., "20251129140000_SigningKeySeeder")
    /// </summary>
    string Name { get; }
}
