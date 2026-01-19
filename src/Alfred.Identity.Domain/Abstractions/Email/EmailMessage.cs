namespace Alfred.Identity.Domain.Abstractions.Email;

/// <summary>
/// Represents an email message to be sent
/// </summary>
public sealed record EmailMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string To { get; init; } = string.Empty;
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? PlainTextBody { get; init; }
    public string? FromEmail { get; init; }
    public string? FromName { get; init; }
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;
    public Dictionary<string, string> Metadata { get; init; } = new();
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public int RetryCount { get; init; } = 0;
    public int MaxRetries { get; init; } = 3;
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Result of sending an email
/// </summary>
public sealed record EmailSendResult
{
    public bool Success { get; init; }
    public string? MessageId { get; init; }
    public string? ErrorMessage { get; init; }
    public string? ProviderResponse { get; init; }

    public static EmailSendResult Succeeded(string? messageId = null, string? providerResponse = null)
    {
        return new EmailSendResult { Success = true, MessageId = messageId, ProviderResponse = providerResponse };
    }

    public static EmailSendResult Failed(string errorMessage, string? providerResponse = null)
    {
        return new EmailSendResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ProviderResponse = providerResponse
        };
    }
}
