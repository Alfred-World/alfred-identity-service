using Alfred.Identity.Domain.Abstractions;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Alfred.Identity.Infrastructure.Providers.Cache;

/// <summary>
/// Redis implementation of ICacheProvider for production use.
/// Provides distributed caching across multiple service instances.
/// </summary>
public sealed class RedisCacheProvider : ICacheProvider
{
    private readonly IConnectionMultiplexer _connection;
    private readonly IDatabase _database;
    private readonly ILogger<RedisCacheProvider> _logger;

    public RedisCacheProvider(IConnectionMultiplexer connection, ILogger<RedisCacheProvider> logger)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _database = _connection.GetDatabase();
    }

    public async ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting key {Key} from Redis", key);
            return null;
        }
    }

    public async ValueTask<bool> SetAsync(
        string key,
        string value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.StringSetAsync(key, value, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting key {Key} in Redis", key);
            return false;
        }
    }

    public async ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key {Key} from Redis", key);
            return false;
        }
    }

    public async ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExistsAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of key {Key} in Redis", key);
            return false;
        }
    }

    public async ValueTask<bool> ExpireAsync(string key, TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.KeyExpireAsync(key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting expiration for key {Key} in Redis", key);
            return false;
        }
    }

    // List operations
    public async ValueTask<long> ListRightPushAsync(string key, string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.ListRightPushAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pushing to list {Key} in Redis", key);
            return 0;
        }
    }

    public async ValueTask<string?> ListLeftPopAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.ListLeftPopAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error popping from list {Key} in Redis", key);
            return null;
        }
    }

    public async ValueTask<long> ListLengthAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.ListLengthAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting length of list {Key} in Redis", key);
            return 0;
        }
    }

    public async ValueTask<IReadOnlyList<string>> ListRangeAsync(
        string key,
        long start = 0,
        long stop = -1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var values = await _database.ListRangeAsync(key, start, stop);
            return values.Select(v => v.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting range from list {Key} in Redis", key);
            return Array.Empty<string>();
        }
    }

    // Hash operations
    public async ValueTask<bool> HashSetAsync(
        string key,
        string field,
        string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.HashSetAsync(key, field, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting hash field {Field} in {Key} in Redis", field, key);
            return false;
        }
    }

    public async ValueTask<string?> HashGetAsync(string key, string field,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _database.HashGetAsync(key, field);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hash field {Field} from {Key} in Redis", field, key);
            return null;
        }
    }

    public async ValueTask<bool> HashDeleteAsync(string key, string field,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.HashDeleteAsync(key, field);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting hash field {Field} from {Key} in Redis", field, key);
            return false;
        }
    }

    public async ValueTask<Dictionary<string, string>> HashGetAllAsync(string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _database.HashGetAllAsync(key);
            return entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all hash fields from {Key} in Redis", key);
            return new Dictionary<string, string>();
        }
    }

    // Set operations
    public async ValueTask<bool> SetAddAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.SetAddAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding to set {Key} in Redis", key);
            return false;
        }
    }

    public async ValueTask<bool> SetRemoveAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.SetRemoveAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing from set {Key} in Redis", key);
            return false;
        }
    }

    public async ValueTask<bool> SetContainsAsync(string key, string value,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _database.SetContainsAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking set membership for {Key} in Redis", key);
            return false;
        }
    }

    public async ValueTask<IReadOnlyList<string>> SetMembersAsync(string key,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await _database.SetMembersAsync(key);
            return members.Select(m => m.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting set members for {Key} in Redis", key);
            return Array.Empty<string>();
        }
    }
}
