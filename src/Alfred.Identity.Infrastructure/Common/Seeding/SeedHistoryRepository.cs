using System.Data;

using Microsoft.EntityFrameworkCore;

using Npgsql;

namespace Alfred.Identity.Infrastructure.Common.Seeding;

/// <summary>
/// PostgreSQL implementation of ISeedHistoryRepository.
/// Persists seed execution records to the __seed_history table,
/// creating the table automatically on first use.
/// </summary>
internal sealed class SeedHistoryRepository : ISeedHistoryRepository
{
    private readonly IDbContext _dbContext;
    private bool _tableEnsured;

    public SeedHistoryRepository(IDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> HasBeenExecutedAsync(string seederName, CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);

        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          SELECT COUNT(1) FROM __seed_history
                          WHERE seeder_name = @name AND success = TRUE
                          """;
        cmd.Parameters.AddWithValue("name", seederName);

        var count = (long)(await cmd.ExecuteScalarAsync(cancellationToken))!;
        return count > 0;
    }

    public async Task RecordExecutionAsync(SeedHistory history, CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);

        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          INSERT INTO __seed_history
                              (seeder_name, executed_at, executed_by, success, error_message, duration_ms)
                          VALUES
                              (@name, @at, @by, @success, @error, @duration)
                          """;
        cmd.Parameters.AddWithValue("name", history.SeederName);
        cmd.Parameters.AddWithValue("at", history.ExecutedAt);
        cmd.Parameters.AddWithValue("by", history.ExecutedBy);
        cmd.Parameters.AddWithValue("success", history.Success);
        cmd.Parameters.AddWithValue("error", (object?)history.ErrorMessage ?? DBNull.Value);
        cmd.Parameters.AddWithValue("duration", (long)history.Duration.TotalMilliseconds);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<List<SeedHistory>> GetAllHistoryAsync(CancellationToken cancellationToken = default)
    {
        await EnsureTableExistsAsync(cancellationToken);

        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          SELECT id, seeder_name, executed_at, executed_by, success, error_message, duration_ms
                          FROM __seed_history
                          ORDER BY executed_at DESC
                          """;

        var results = new List<SeedHistory>();
        using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SeedHistory
            {
                Id = reader.GetInt64(0),
                SeederName = reader.GetString(1),
                ExecutedAt = reader.GetDateTime(2),
                ExecutedBy = reader.GetString(3),
                Success = reader.GetBoolean(4),
                ErrorMessage = reader.IsDBNull(5) ? null : reader.GetString(5),
                Duration = TimeSpan.FromMilliseconds(reader.GetInt64(6))
            });
        }

        return results;
    }

    private async Task EnsureTableExistsAsync(CancellationToken cancellationToken)
    {
        if (_tableEnsured)
        {
            return;
        }

        var connection = (NpgsqlConnection)_dbContext.Database.GetDbConnection();
        await EnsureOpenAsync(connection, cancellationToken);

        using var cmd = connection.CreateCommand();
        cmd.CommandText = """
                          CREATE TABLE IF NOT EXISTS __seed_history (
                              id            BIGSERIAL PRIMARY KEY,
                              seeder_name   VARCHAR(500) NOT NULL,
                              executed_at   TIMESTAMPTZ  NOT NULL DEFAULT NOW(),
                              executed_by   VARCHAR(200) NOT NULL DEFAULT 'System',
                              success       BOOLEAN      NOT NULL,
                              error_message TEXT,
                              duration_ms   BIGINT       NOT NULL DEFAULT 0
                          );
                          CREATE UNIQUE INDEX IF NOT EXISTS ux_seed_history_name_success
                              ON __seed_history (seeder_name)
                              WHERE success = TRUE;
                          """;

        await cmd.ExecuteNonQueryAsync(cancellationToken);
        _tableEnsured = true;
    }

    private static async Task EnsureOpenAsync(NpgsqlConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct);
        }
    }
}
