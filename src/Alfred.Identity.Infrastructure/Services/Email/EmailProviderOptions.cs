namespace Alfred.Identity.Infrastructure.Services.Email;

/// <summary>
/// Email provider configuration options
/// </summary>
public sealed class EmailProviderOptions
{
    public const string SectionName = "Email";

    /// <summary>
    /// Active provider: "Brevo", "Smtp", "SendGrid", etc.
    /// </summary>
    public string Provider { get; set; } = "Smtp";

    /// <summary>
    /// Default sender email
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Default sender name
    /// </summary>
    public string FromName { get; set; } = "FAM System";

    /// <summary>
    /// SMTP specific settings
    /// </summary>
    public SmtpOptions Smtp { get; set; } = new();
}

/// <summary>
/// SMTP provider options
/// </summary>
public sealed class SmtpOptions
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}
