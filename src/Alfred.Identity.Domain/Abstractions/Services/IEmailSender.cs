namespace Alfred.Identity.Domain.Abstractions.Services;

public interface IEmailSender
{
    Task SendEmailAsync(
        string to,
        string subject,
        string htmlBody,
        string? templateCode = null,
        object? templateParams = null,
        CancellationToken cancellationToken = default);
}
