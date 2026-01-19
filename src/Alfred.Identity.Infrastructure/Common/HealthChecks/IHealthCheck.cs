namespace Alfred.Identity.Infrastructure.Common.HealthChecks;

/// <summary>
/// Interface for service health checks (Database, Redis, RabbitMQ, etc.)
/// </summary>
public interface IHealthCheck
{
    /// <summary>
    /// Name of the service being checked
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// Check if the service is healthy and can be connected
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
