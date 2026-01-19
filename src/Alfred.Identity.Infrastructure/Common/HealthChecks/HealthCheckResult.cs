namespace Alfred.Identity.Infrastructure.Common.HealthChecks;

/// <summary>
/// Health check result status
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

/// <summary>
/// Result of a health check operation
/// </summary>
public class HealthCheckResult
{
    public string ServiceName { get; init; } = string.Empty;
    public HealthStatus Status { get; init; }
    public string? Description { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }

    public static HealthCheckResult Healthy(string serviceName, string? description = null, TimeSpan duration = default)
    {
        return new HealthCheckResult
        {
            ServiceName = serviceName,
            Status = HealthStatus.Healthy,
            Description = description,
            Duration = duration
        };
    }

    public static HealthCheckResult Degraded(string serviceName, string? description = null,
        TimeSpan duration = default)
    {
        return new HealthCheckResult
        {
            ServiceName = serviceName,
            Status = HealthStatus.Degraded,
            Description = description,
            Duration = duration
        };
    }

    public static HealthCheckResult Unhealthy(string serviceName, string errorMessage, TimeSpan duration = default)
    {
        return new HealthCheckResult
        {
            ServiceName = serviceName,
            Status = HealthStatus.Unhealthy,
            ErrorMessage = errorMessage,
            Duration = duration
        };
    }
}
