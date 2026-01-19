namespace Alfred.Identity.Domain.Abstractions.Email;

/// <summary>
/// Email provider abstraction - implement this interface to add new email providers
/// Examples: SMTP, SendGrid, Mailgun, AWS SES, etc.
/// </summary>
public interface IEmailProvider
{
    /// <summary>
    /// Provider name for logging and identification
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Check if the provider is properly configured and available
    /// </summary>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an email message
    /// </summary>
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send multiple emails in batch (if supported by provider)
    /// Default implementation sends one by one
    /// </summary>
    async Task<IReadOnlyList<EmailSendResult>> SendBatchAsync(
        IEnumerable<EmailMessage> messages,
        CancellationToken cancellationToken = default)
    {
        List<EmailSendResult> results = new();
        foreach (var message in messages)
        {
            var result = await SendAsync(message, cancellationToken);
            results.Add(result);
        }

        return results;
    }
}
