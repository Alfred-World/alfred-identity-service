using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Email;

using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Services.Email;

/// <summary>
/// Cache-backed email queue for persistence across app restarts
/// Uses cache provider abstraction to support multiple cache implementations (Redis, Memcached, etc.)
/// </summary>
public sealed class CacheEmailQueue : IEmailQueue
{
    private readonly ICacheProvider _cache;
    private readonly ILogger<CacheEmailQueue> _logger;

    private const string QueueKey = "fam:email:queue";
    private const string ProcessingKey = "fam:email:processing";
    private const string FailedKey = "fam:email:failed";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public CacheEmailQueue(ICacheProvider cache, ILogger<CacheEmailQueue> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public int Count
    {
        get
        {
            try
            {
                return (int)_cache.ListLengthAsync(QueueKey).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get queue count from cache");
                return 0;
            }
        }
    }

    public async ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        try
        {
            var json = JsonSerializer.Serialize(message, JsonOptions);

            // RPUSH for FIFO queue (add to right, take from left)
            await _cache.ListRightPushAsync(QueueKey, json, cancellationToken);

            _logger.LogDebug(
                "Email queued to cache: {EmailId} to {To}, Priority: {Priority}, Queue size: {Count}",
                message.Id, message.To, message.Priority, Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue email {EmailId} to cache", message.Id);
            throw;
        }
    }

    public async ValueTask<EmailMessage?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // LPOP to get from left (FIFO)
            var json = await _cache.ListLeftPopAsync(QueueKey, cancellationToken);

            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            var message = JsonSerializer.Deserialize<EmailMessage>(json, JsonOptions);

            if (message != null)
            {
                _logger.LogDebug(
                    "Email dequeued from cache: {EmailId} to {To}, Queue size: {Count}",
                    message.Id, message.To, Count);
            }

            return message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dequeue email from cache");
            return null;
        }
    }

    /// <summary>
    /// Move failed email to failed queue for manual review
    /// </summary>
    public async Task MoveToFailedAsync(EmailMessage message, string errorMessage)
    {
        try
        {
            var failedMessage = new
            {
                Email = message,
                Error = errorMessage,
                FailedAt = DateTime.UtcNow
            };
            var json = JsonSerializer.Serialize(failedMessage, JsonOptions);

            await _cache.ListRightPushAsync(FailedKey, json);

            _logger.LogWarning(
                "Email {EmailId} moved to failed queue: {Error}",
                message.Id, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move email {EmailId} to failed queue", message.Id);
        }
    }

    /// <summary>
    /// Get count of failed emails
    /// </summary>
    public int FailedCount
    {
        get
        {
            try
            {
                return (int)_cache.ListLengthAsync(FailedKey).GetAwaiter().GetResult();
            }
            catch
            {
                return 0;
            }
        }
    }

    /// <summary>
    /// Retry all failed emails by moving them back to the main queue
    /// </summary>
    public async Task<int> RetryFailedEmailsAsync()
    {
        var retried = 0;

        while (true)
        {
            var json = await _cache.ListLeftPopAsync(FailedKey);
            if (string.IsNullOrEmpty(json))
            {
                break;
            }

            // Extract just the email part and re-queue
            try
            {
                using var doc = JsonDocument.Parse(json);
                var emailJson = doc.RootElement.GetProperty("email").GetRawText();
                await _cache.ListRightPushAsync(QueueKey, emailJson);
                retried++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry email from failed queue");
            }
        }

        return retried;
    }
}
