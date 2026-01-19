using System.Net;
using System.Net.Mail;
using System.Text;

using Alfred.Identity.Domain.Abstractions.Email;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alfred.Identity.Infrastructure.Services.Email.Providers;

/// <summary>
/// SMTP email provider implementation
/// Fallback provider that works with any SMTP server
/// </summary>
public sealed class SmtpEmailProvider : IEmailProvider
{
    private readonly ILogger<SmtpEmailProvider> _logger;
    private readonly SmtpOptions _options;
    private readonly string _defaultFromEmail;
    private readonly string _defaultFromName;

    public string ProviderName => "SMTP";

    public SmtpEmailProvider(
        IOptions<EmailProviderOptions> options,
        ILogger<SmtpEmailProvider> logger)
    {
        _logger = logger;
        _options = options.Value.Smtp;
        _defaultFromEmail = options.Value.FromEmail;
        _defaultFromName = options.Value.FromName;
    }

    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        var isConfigured = !string.IsNullOrEmpty(_options.Host)
                           && !string.IsNullOrEmpty(_options.Username)
                           && !string.IsNullOrEmpty(_options.Password);

        if (!isConfigured)
        {
            _logger.LogWarning("SMTP is not properly configured");
        }

        return Task.FromResult(isConfigured);
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if SMTP is configured
            if (!await IsAvailableAsync(cancellationToken))
            {
                _logger.LogWarning(
                    "SMTP not configured. Email would be sent to {Email} with subject: {Subject}",
                    message.To, message.Subject);

                // Return success in development mode to not block the flow
                return EmailSendResult.Succeeded("dev-mode-skipped");
            }

            using SmtpClient smtpClient = new(_options.Host, _options.Port)
            {
                Credentials = new NetworkCredential(_options.Username, _options.Password),
                EnableSsl = _options.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var fromEmail = message.FromEmail ?? _defaultFromEmail;
            var fromName = message.FromName ?? _defaultFromName;

            using MailMessage mailMessage = new()
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = message.Subject,
                Body = message.HtmlBody,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };

            mailMessage.To.Add(message.To);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);


            return EmailSendResult.Succeeded();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP to {Email}", message.To);
            return EmailSendResult.Failed(ex.Message);
        }
    }
}
