namespace Alfred.Identity.Domain.Abstractions.Email;

/// <summary>
/// Email queue service - queues emails for background processing
/// Decouples email sending from the main request flow
/// </summary>
public interface IEmailQueue
{
    /// <summary>
    /// Queue an email for sending
    /// </summary>
    ValueTask EnqueueAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dequeue the next email to send
    /// </summary>
    ValueTask<EmailMessage?> DequeueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current queue length
    /// </summary>
    int Count { get; }
}
