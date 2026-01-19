using Alfred.Identity.Domain.Abstractions.Email;

using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Services.Email;

/// <summary>
/// Email service implementation that queues emails for background processing
/// This decouples email sending from the main request flow
/// </summary>
public sealed class QueuedEmailService : IEmailService
{
    private readonly IEmailQueue _emailQueue;
    private readonly IEmailTemplateService _templateService;
    private readonly ILogger<QueuedEmailService> _logger;

    public QueuedEmailService(
        IEmailQueue emailQueue,
        IEmailTemplateService templateService,
        ILogger<QueuedEmailService> logger)
    {
        _emailQueue = emailQueue;
        _templateService = templateService;
        _logger = logger;
    }

    public async Task SendOtpEmailAsync(string toEmail, string otpCode, string userName,
        CancellationToken cancellationToken = default)
    {
        var message = _templateService.CreateOtpEmail(toEmail, userName, otpCode);
        await _emailQueue.EnqueueAsync(message, cancellationToken);

        _logger.LogDebug("OTP email queued successfully. Queue size: {QueueSize}", _emailQueue.Count);
    }

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName, string resetUrl,
        int expiryMinutes, CancellationToken cancellationToken = default)
    {
        var message =
            _templateService.CreatePasswordResetEmail(toEmail, userName, resetToken, resetUrl, expiryMinutes);
        await _emailQueue.EnqueueAsync(message, cancellationToken);

        _logger.LogDebug("Password reset email queued successfully. Queue size: {QueueSize}", _emailQueue.Count);
    }

    public async Task SendPasswordChangedEmailAsync(string toEmail, string userName,
        CancellationToken cancellationToken = default)
    {
        var message = _templateService.CreatePasswordChangedEmail(toEmail, userName);
        await _emailQueue.EnqueueAsync(message, cancellationToken);

        _logger.LogDebug("Password changed email queued successfully. Queue size: {QueueSize}", _emailQueue.Count);
    }
}
