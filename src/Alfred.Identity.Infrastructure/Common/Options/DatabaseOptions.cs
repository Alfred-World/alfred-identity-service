namespace Alfred.Identity.Infrastructure.Common.Options;

/// <summary>
/// Supported database providers
/// </summary>
public enum DatabaseProvider
{
    PostgreSQL
}

/// <summary>
/// PostgreSQL specific options
/// </summary>
public class PostgreSqlOptions
{
    public string ConnectionString { get; set; } =
        "Host=localhost;Database=alfred_identity;Username=alfred;Password=alfred123;";

    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
}
