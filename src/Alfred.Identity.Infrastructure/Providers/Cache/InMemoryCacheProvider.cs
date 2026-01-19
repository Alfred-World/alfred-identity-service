using System.Collections.Concurrent;
using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;

using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Providers.Cache;

/// <summary>
/// In-memory implementation of ICacheProvider for development/testing
/// Uses ConcurrentDictionary for thread-safe operations
/// Note: Data is lost on application restart
/// </summary>
public sealed class InMemoryCacheProvider : ICacheProvider
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly ILogger<InMemoryCacheProvider> _logger;

    private record CacheEntry(string Value, DateTime? ExpiresAt);

    public InMemoryCacheProvider(ILogger<InMemoryCacheProvider> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt.HasValue && entry.ExpiresAt.Value < DateTime.UtcNow)
            {
                _cache.TryRemove(key, out _);
                return ValueTask.FromResult<string?>(null);
            }

            return ValueTask.FromResult<string?>(entry.Value);
        }

        return ValueTask.FromResult<string?>(null);
    }

    public ValueTask<bool> SetAsync(
        string key,
        string value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : (DateTime?)null;
        _cache[key] = new CacheEntry(value, expiresAt);
        return ValueTask.FromResult(true);
    }

    public ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_cache.TryRemove(key, out _));
    }

    public async ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(key, cancellationToken);
        return value != null;
    }

    public ValueTask<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            var expiresAt = DateTime.UtcNow.Add(expiration);
            _cache[key] = entry with { ExpiresAt = expiresAt };
            return ValueTask.FromResult(true);
        }

        return ValueTask.FromResult(false);
    }

    // List operations - stored as JSON array in a single key
    public async ValueTask<long> ListRightPushAsync(string key, string value,
        CancellationToken cancellationToken = default)
    {
        var list = await GetListAsync(key);
        list.Add(value);
        await SetListAsync(key, list);
        return list.Count;
    }

    public async ValueTask<string?> ListLeftPopAsync(string key, CancellationToken cancellationToken = default)
    {
        var list = await GetListAsync(key);
        if (list.Count == 0)
        {
            return null;
        }

        var value = list[0];
        list.RemoveAt(0);
        await SetListAsync(key, list);
        return value;
    }

    public async ValueTask<long> ListLengthAsync(string key, CancellationToken cancellationToken = default)
    {
        var list = await GetListAsync(key);
        return list.Count;
    }

    public async ValueTask<IReadOnlyList<string>> ListRangeAsync(
        string key,
        long start = 0,
        long stop = -1,
        CancellationToken cancellationToken = default)
    {
        var list = await GetListAsync(key);
        if (list.Count == 0)
        {
            return Array.Empty<string>();
        }

        var startIdx = (int)Math.Max(0, start);
        var endIdx = stop == -1 ? list.Count - 1 : (int)Math.Min(list.Count - 1, stop);

        if (startIdx >= list.Count)
        {
            return Array.Empty<string>();
        }

        var count = endIdx - startIdx + 1;
        return list.Skip(startIdx).Take(count).ToList();
    }

    private async ValueTask<List<string>> GetListAsync(string key)
    {
        var json = await GetAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async ValueTask SetListAsync(string key, List<string> list)
    {
        var json = JsonSerializer.Serialize(list);
        await SetAsync(key, json);
    }

    // Hash operations - stored as JSON dictionary
    public async ValueTask<bool> HashSetAsync(
        string key,
        string field,
        string value,
        CancellationToken cancellationToken = default)
    {
        var hash = await GetHashAsync(key);
        hash[field] = value;
        await SetHashAsync(key, hash);
        return true;
    }

    public async ValueTask<string?> HashGetAsync(string key, string field,
        CancellationToken cancellationToken = default)
    {
        var hash = await GetHashAsync(key);
        return hash.TryGetValue(field, out var value) ? value : null;
    }

    public async ValueTask<bool> HashDeleteAsync(string key, string field,
        CancellationToken cancellationToken = default)
    {
        var hash = await GetHashAsync(key);
        var removed = hash.Remove(field);
        if (removed)
        {
            await SetHashAsync(key, hash);
        }

        return removed;
    }

    public async ValueTask<Dictionary<string, string>> HashGetAllAsync(string key,
        CancellationToken cancellationToken = default)
    {
        return await GetHashAsync(key);
    }

    private async ValueTask<Dictionary<string, string>> GetHashAsync(string key)
    {
        var json = await GetAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private async ValueTask SetHashAsync(string key, Dictionary<string, string> hash)
    {
        var json = JsonSerializer.Serialize(hash);
        await SetAsync(key, json);
    }

    // Set operations - stored as JSON array with unique values
    public async ValueTask<bool> SetAddAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var set = await GetSetAsync(key);
        var added = set.Add(value);
        if (added)
        {
            await SetSetAsync(key, set);
        }

        return added;
    }

    public async ValueTask<bool> SetRemoveAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var set = await GetSetAsync(key);
        var removed = set.Remove(value);
        if (removed)
        {
            await SetSetAsync(key, set);
        }

        return removed;
    }

    public async ValueTask<bool> SetContainsAsync(string key, string value,
        CancellationToken cancellationToken = default)
    {
        var set = await GetSetAsync(key);
        return set.Contains(value);
    }

    public async ValueTask<IReadOnlyList<string>> SetMembersAsync(string key,
        CancellationToken cancellationToken = default)
    {
        var set = await GetSetAsync(key);
        return set.ToList();
    }

    private async ValueTask<HashSet<string>> GetSetAsync(string key)
    {
        var json = await GetAsync(key);
        if (string.IsNullOrEmpty(json))
        {
            return new HashSet<string>();
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list != null ? new HashSet<string>(list) : new HashSet<string>();
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private async ValueTask SetSetAsync(string key, HashSet<string> set)
    {
        var json = JsonSerializer.Serialize(set.ToList());
        await SetAsync(key, json);
    }
}
