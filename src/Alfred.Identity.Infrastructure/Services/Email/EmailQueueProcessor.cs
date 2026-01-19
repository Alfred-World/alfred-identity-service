using Alfred.Identity.Domain.Abstractions.Email;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Services.Email;

/// <summary>
/// Background service that processes the email queue
/// Runs continuously and sends queued emails using the configured provider
/// </summary>
public sealed class EmailQueueProcessor : BackgroundService
{
    private readonly IEmailQueue _emailQueue;
    private readonly IEmailProvider _emailProvider;
    private readonly ILogger<EmailQueueProcessor> _logger;

    // Configuration
    private readonly TimeSpan _processingDelay = TimeSpan.FromMilliseconds(100);
    private readonly TimeSpan _retryDelay = TimeSpan.FromSeconds(5);
    private readonly TimeSpan _emptyQueueDelay = TimeSpan.FromSeconds(1);

    public EmailQueueProcessor(
        IEmailQueue emailQueue,
        IEmailProvider emailProvider,
        ILogger<EmailQueueProcessor> logger)
    {
        _emailQueue = emailQueue;
        _emailProvider = emailProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await _emailQueue.DequeueAsync(stoppingToken);

                if (message == null)
                {
                    // Queue is empty, wait before checking again
                    await Task.Delay(_emptyQueueDelay, stoppingToken);
                    continue;
                }

                await ProcessEmailAsync(message, stoppingToken);

                // Small delay between processing emails to avoid overwhelming the provider
                await Task.Delay(_processingDelay, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email queue processor");
                await Task.Delay(_retryDelay, stoppingToken);
            }
        }
    }

    private async Task ProcessEmailAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Processing email {EmailId} to {To}, Attempt: {Attempt}/{MaxRetries}",
            message.Id, message.To, message.RetryCount + 1, message.MaxRetries);

        var result = await _emailProvider.SendAsync(message, cancellationToken);

        if (result.Success)
        {
            _logger.LogInformation(
                "Email {EmailId} sent successfully to {To}. MessageId: {MessageId}",
                message.Id, message.To, result.MessageId);
        }
        else
        {
            _logger.LogWarning(
                "Failed to send email {EmailId} to {To}: {Error}",
                message.Id, message.To, result.ErrorMessage);

            // Retry logic
            if (message.RetryCount < message.MaxRetries)
            {
                var retryMessage = message with { RetryCount = message.RetryCount + 1 };
                await _emailQueue.EnqueueAsync(retryMessage, cancellationToken);

                _logger.LogInformation(
                    "Email {EmailId} re-queued for retry ({RetryCount}/{MaxRetries})",
                    message.Id, retryMessage.RetryCount, message.MaxRetries);
            }
            else
            {
                _logger.LogError(
                    "Email {EmailId} to {To} failed after {MaxRetries} attempts. Giving up.",
                    message.Id, message.To, message.MaxRetries);

                // TODO: Optionally save to dead letter queue or database for manual review
            }
        }
    }
}
