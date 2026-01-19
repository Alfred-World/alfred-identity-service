namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Abstraction for cache operations to support multiple cache implementations
/// (Redis, Memcached, in-memory, etc.) without changing business logic
/// </summary>
public interface ICacheProvider
{
    /// <summary>
    /// Get a value from cache by key
    /// </summary>
    ValueTask<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set a value in cache with optional expiration
    /// </summary>
    ValueTask<bool> SetAsync(
        string key,
        string value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a value from cache
    /// </summary>
    ValueTask<bool> DeleteAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a key exists in cache
    /// </summary>
    ValueTask<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Set expiration time for a key
    /// </summary>
    ValueTask<bool> ExpireAsync(string key, TimeSpan expiration, CancellationToken cancellationToken = default);

    // List operations for queue implementation
    /// <summary>
    /// Push a value to the right (end) of a list
    /// </summary>
    ValueTask<long> ListRightPushAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pop a value from the left (start) of a list
    /// </summary>
    ValueTask<string?> ListLeftPopAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the length of a list
    /// </summary>
    ValueTask<long> ListLengthAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a range of values from a list
    /// </summary>
    ValueTask<IReadOnlyList<string>> ListRangeAsync(
        string key,
        long start = 0,
        long stop = -1,
        CancellationToken cancellationToken = default);

    // Hash operations for structured data
    /// <summary>
    /// Set a field in a hash
    /// </summary>
    ValueTask<bool> HashSetAsync(
        string key,
        string field,
        string value,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a field from a hash
    /// </summary>
    ValueTask<string?> HashGetAsync(string key, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a field from a hash
    /// </summary>
    ValueTask<bool> HashDeleteAsync(string key, string field, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all fields and values from a hash
    /// </summary>
    ValueTask<Dictionary<string, string>> HashGetAllAsync(string key, CancellationToken cancellationToken = default);

    // Set operations for unique collections
    /// <summary>
    /// Add a member to a set
    /// </summary>
    ValueTask<bool> SetAddAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a member from a set
    /// </summary>
    ValueTask<bool> SetRemoveAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a member exists in a set
    /// </summary>
    ValueTask<bool> SetContainsAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all members of a set
    /// </summary>
    ValueTask<IReadOnlyList<string>> SetMembersAsync(string key, CancellationToken cancellationToken = default);
}
