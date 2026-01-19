using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Common.HealthChecks;

/// <summary>
/// Orchestrates all health checks for startup validation
/// Provides detailed reporting of which services are unavailable
/// </summary>
public class HealthCheckOrchestrator
{
    private readonly IEnumerable<IHealthCheck> _healthChecks;
    private readonly ILogger<HealthCheckOrchestrator> _logger;

    public HealthCheckOrchestrator(IEnumerable<IHealthCheck> healthChecks, ILogger<HealthCheckOrchestrator> logger)
    {
        _healthChecks = healthChecks;
        _logger = logger;
    }

    /// <summary>
    /// Run all health checks and validate all services are available
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all services are healthy, false otherwise</returns>
    public async Task<bool> ValidateAllServicesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting service validation...");

        List<HealthCheckResult> results = new();
        var overallStartTime = DateTime.UtcNow;

        foreach (var healthCheck in _healthChecks)
        {
            try
            {
                var result = await healthCheck.CheckHealthAsync(cancellationToken);
                results.Add(result);

                // Log individual result
                if (result.Status == HealthStatus.Healthy)
                {
                    _logger.LogInformation("{ServiceName} is healthy ({DurationMs}ms). {Description}",
                        result.ServiceName, result.Duration.TotalMilliseconds, result.Description);
                }
                else if (result.Status == HealthStatus.Degraded)
                {
                    _logger.LogWarning("{ServiceName} is degraded ({DurationMs}ms). {Description}",
                        result.ServiceName, result.Duration.TotalMilliseconds, result.Description);
                }
                else
                {
                    _logger.LogError("{ServiceName} is unhealthy ({DurationMs}ms). Error: {ErrorMessage}",
                        result.ServiceName, result.Duration.TotalMilliseconds, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                var errorResult = HealthCheckResult.Unhealthy(
                    healthCheck.ServiceName,
                    $"Health check failed: {ex.Message}");

                results.Add(errorResult);
                _logger.LogError(ex, "{ServiceName} health check failed: {Message}",
                    errorResult.ServiceName, ex.Message);
            }
        }

        var overallDuration = DateTime.UtcNow - overallStartTime;

        // Summary
        var healthyCount = results.Count(r => r.Status == HealthStatus.Healthy);
        var degradedCount = results.Count(r => r.Status == HealthStatus.Degraded);
        var unhealthyCount = results.Count(r => r.Status == HealthStatus.Unhealthy);
        var totalCount = results.Count;

        var allHealthy = unhealthyCount == 0;

        if (allHealthy)
        {
            _logger.LogInformation("All services are healthy ({HealthyCount}/{TotalCount}) - Total: {DurationMs}ms",
                healthyCount, totalCount, overallDuration.TotalMilliseconds);

            if (degradedCount > 0)
            {
                _logger.LogWarning("{DegradedCount} service(s) degraded", degradedCount);
            }
        }
        else
        {
            _logger.LogCritical("Service validation FAILED - {UnhealthyCount}/{TotalCount} service(s) unavailable",
                unhealthyCount, totalCount);
            _logger.LogCritical("Health: {HealthyCount}, Degraded: {DegradedCount}, Unhealthy: {UnhealthyCount}",
                healthyCount, degradedCount, unhealthyCount);

            foreach (var result in results.Where(r => r.Status == HealthStatus.Unhealthy))
            {
                _logger.LogCritical("{ServiceName}: {ErrorMessage}", result.ServiceName, result.ErrorMessage);
            }
        }

        return allHealthy;
    }

    /// <summary>
    /// Get current health status of all services
    /// </summary>
    public async Task<IEnumerable<HealthCheckResult>> GetHealthStatusAsync(
        CancellationToken cancellationToken = default)
    {
        List<HealthCheckResult> results = new();

        foreach (var healthCheck in _healthChecks)
        {
            try
            {
                var result = await healthCheck.CheckHealthAsync(cancellationToken);
                results.Add(result);
            }
            catch (Exception ex)
            {
                results.Add(HealthCheckResult.Unhealthy(
                    healthCheck.ServiceName,
                    $"Health check failed: {ex.Message}"));
            }
        }

        return results;
    }
}
