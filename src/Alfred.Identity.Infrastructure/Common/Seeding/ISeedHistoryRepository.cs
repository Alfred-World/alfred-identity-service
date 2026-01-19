namespace Alfred.Identity.Infrastructure.Common.Seeding;

/// <summary>
/// Repository for managing seed history
/// </summary>
public interface ISeedHistoryRepository
{
    Task<bool> HasBeenExecutedAsync(string seederName, CancellationToken cancellationToken = default);
    Task RecordExecutionAsync(SeedHistory history, CancellationToken cancellationToken = default);
    Task<List<SeedHistory>> GetAllHistoryAsync(CancellationToken cancellationToken = default);
}
