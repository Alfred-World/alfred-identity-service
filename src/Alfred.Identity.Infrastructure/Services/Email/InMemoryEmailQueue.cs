using System.Threading.Channels;

using Alfred.Identity.Domain.Abstractions.Email;

using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Services.Email;

/// <summary>
/// In-memory email queue using System.Threading.Channels
/// For production with multiple instances, consider using Redis, RabbitMQ, or Azure Service Bus
/// </summary>
public sealed class InMemoryEmailQueue : IEmailQueue
{
    private readonly Channel<EmailMessage> _queue;
    private readonly ILogger<InMemoryEmailQueue> _logger;
    private int _count;

    public InMemoryEmailQueue(ILogger<InMemoryEmailQueue> logger, int capacity = 1000)
    {
        _logger = logger;

        // Bounded channel with configurable capacity
        // When full, oldest messages will be dropped (for resilience)
        BoundedChannelOptions options = new(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = false,
            SingleWriter = false
        };

        _queue = Channel.CreateBounded<EmailMessage>(options);
    }

    public int Count => _count;

    public async ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        await _queue.Writer.WriteAsync(message, cancellationToken);
        Interlocked.Increment(ref _count);

        _logger.LogDebug(
            "Email queued: {EmailId} to {To}, Priority: {Priority}, Queue size: {Count}",
            message.Id, message.To, message.Priority, _count);
    }

    public async ValueTask<EmailMessage?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var message = await _queue.Reader.ReadAsync(cancellationToken);
            Interlocked.Decrement(ref _count);

            _logger.LogDebug(
                "Email dequeued: {EmailId} to {To}, Queue size: {Count}",
                message.Id, message.To, _count);

            return message;
        }
        catch (ChannelClosedException)
        {
            return null;
        }
    }
}
