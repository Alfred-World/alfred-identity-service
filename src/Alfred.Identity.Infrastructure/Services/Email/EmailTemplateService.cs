using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Email;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Alfred.Identity.Infrastructure.Services.Email;

/// <summary>
/// Email template service - loads email templates from database and replaces placeholders
/// Templates are managed via EmailTemplate entity and can be customized through Admin API
/// </summary>
public sealed class EmailTemplateService : IEmailTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _fromEmail;
    private readonly string _fromName;

    public EmailTemplateService(
        IUnitOfWork unitOfWork,
        ILogger<EmailTemplateService> logger,
        IOptions<EmailProviderOptions> options)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _fromEmail = options.Value.FromEmail;
        _fromName = options.Value.FromName;
    }

    public EmailMessage CreateOtpEmail(string toEmail, string userName, string otpCode)
    {
        var template = _unitOfWork.EmailTemplates.GetByCodeAsync("OTP_EMAIL").GetAwaiter().GetResult();
        if (template == null)
        {
            _logger.LogError("Email template OTP_EMAIL not found. Falling back to hardcoded template.");
            return CreateFallbackOtpEmail(toEmail, userName, otpCode);
        }

        Dictionary<string, string> placeholders = new()
        {
            { "userName", userName },
            { "otpCode", otpCode },
            { "expiryMinutes", "10" },
            { "appName", "FAM System" },
            { "currentYear", DateTime.UtcNow.Year.ToString() }
        };

        return new EmailMessage
        {
            To = toEmail,
            Subject = ReplacePlaceholders(template.Subject, placeholders),
            HtmlBody = ReplacePlaceholders(template.HtmlBody, placeholders),
            PlainTextBody = template.PlainTextBody != null
                ? ReplacePlaceholders(template.PlainTextBody, placeholders)
                : null,
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.High,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "otp",
                ["userName"] = userName,
                ["templateCode"] = template.Code
            }
        };
    }

    public EmailMessage CreatePasswordResetEmail(string toEmail, string userName, string resetToken, string resetUrl,
        int expiryMinutes)
    {
        var template = _unitOfWork.EmailTemplates.GetByCodeAsync("PASSWORD_RESET").GetAwaiter().GetResult();
        if (template == null)
        {
            _logger.LogError("Email template PASSWORD_RESET not found. Falling back to hardcoded template.");
            return CreateFallbackPasswordResetEmail(toEmail, userName, resetToken, resetUrl, expiryMinutes);
        }

        var resetLink = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

        Dictionary<string, string> placeholders = new()
        {
            { "userName", userName },
            { "resetLink", resetLink },
            { "expiryMinutes", expiryMinutes.ToString() },
            { "expiryHours", "1" },
            { "appName", "FAM System" },
            { "currentYear", DateTime.UtcNow.Year.ToString() }
        };

        return new EmailMessage
        {
            To = toEmail,
            Subject = ReplacePlaceholders(template.Subject, placeholders),
            HtmlBody = ReplacePlaceholders(template.HtmlBody, placeholders),
            PlainTextBody = template.PlainTextBody != null
                ? ReplacePlaceholders(template.PlainTextBody, placeholders)
                : null,
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.High,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "password_reset",
                ["userName"] = userName,
                ["templateCode"] = template.Code
            }
        };
    }

    public EmailMessage CreatePasswordChangedEmail(string toEmail, string userName)
    {
        var template =
            _unitOfWork.EmailTemplates.GetByCodeAsync("PASSWORD_CHANGED").GetAwaiter().GetResult();
        if (template == null)
        {
            _logger.LogError("Email template PASSWORD_CHANGED not found. Falling back to hardcoded template.");
            return CreateFallbackPasswordChangedEmail(toEmail, userName);
        }

        Dictionary<string, string> placeholders = new()
        {
            { "userName", userName },
            { "changeTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
            { "appName", "FAM System" },
            { "currentYear", DateTime.UtcNow.Year.ToString() }
        };

        return new EmailMessage
        {
            To = toEmail,
            Subject = ReplacePlaceholders(template.Subject, placeholders),
            HtmlBody = ReplacePlaceholders(template.HtmlBody, placeholders),
            PlainTextBody = template.PlainTextBody != null
                ? ReplacePlaceholders(template.PlainTextBody, placeholders)
                : null,
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.Normal,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "password_changed",
                ["userName"] = userName,
                ["templateCode"] = template.Code
            }
        };
    }

    public EmailMessage CreateWelcomeEmail(string toEmail, string userName)
    {
        var template = _unitOfWork.EmailTemplates.GetByCodeAsync("WELCOME_EMAIL").GetAwaiter().GetResult();
        if (template == null)
        {
            _logger.LogError("Email template WELCOME_EMAIL not found. Falling back to hardcoded template.");
            return CreateFallbackWelcomeEmail(toEmail, userName);
        }

        Dictionary<string, string> placeholders = new()
        {
            { "userName", userName },
            { "appName", "FAM System" },
            { "currentYear", DateTime.UtcNow.Year.ToString() }
        };

        return new EmailMessage
        {
            To = toEmail,
            Subject = ReplacePlaceholders(template.Subject, placeholders),
            HtmlBody = ReplacePlaceholders(template.HtmlBody, placeholders),
            PlainTextBody = template.PlainTextBody != null
                ? ReplacePlaceholders(template.PlainTextBody, placeholders)
                : null,
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.Normal,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "welcome",
                ["userName"] = userName,
                ["templateCode"] = template.Code
            }
        };
    }

    public EmailMessage CreateAccountLockedEmail(string toEmail, string userName, string reason)
    {
        var template = _unitOfWork.EmailTemplates.GetByCodeAsync("ACCOUNT_LOCKED").GetAwaiter().GetResult();
        if (template == null)
        {
            _logger.LogError("Email template ACCOUNT_LOCKED not found. Falling back to hardcoded template.");
            return CreateFallbackAccountLockedEmail(toEmail, userName, reason);
        }

        Dictionary<string, string> placeholders = new()
        {
            { "userName", userName },
            { "reason", reason },
            { "lockTime", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") },
            { "appName", "FAM System" },
            { "currentYear", DateTime.UtcNow.Year.ToString() }
        };

        return new EmailMessage
        {
            To = toEmail,
            Subject = ReplacePlaceholders(template.Subject, placeholders),
            HtmlBody = ReplacePlaceholders(template.HtmlBody, placeholders),
            PlainTextBody = template.PlainTextBody != null
                ? ReplacePlaceholders(template.PlainTextBody, placeholders)
                : null,
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.Critical,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "account_locked",
                ["userName"] = userName,
                ["reason"] = reason,
                ["templateCode"] = template.Code
            }
        };
    }

    #region Helper Methods

    /// <summary>
    /// Replace placeholders in template string
    /// Supports both {{placeholder}} and {placeholder} formats
    /// </summary>
    private static string ReplacePlaceholders(string template, Dictionary<string, string> placeholders)
    {
        var result = template;

        foreach (var (key, value) in placeholders)
        {
            // Replace {{key}} format (from database templates)
            result = result.Replace($"{{{{{key}}}}}", value);
            // Replace {key} format (legacy support)
            result = result.Replace($"{{{key}}}", value);
        }

        return result;
    }

    #endregion

    #region Fallback Methods (when templates not found in database)

    private EmailMessage CreateFallbackOtpEmail(string toEmail, string userName, string otpCode)
    {
        return new EmailMessage
        {
            To = toEmail,
            Subject = "Your Verification Code - FAM",
            HtmlBody = GenerateOtpEmailTemplate(userName, otpCode),
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.High,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "otp",
                ["userName"] = userName,
                ["fallback"] = "true"
            }
        };
    }

    private EmailMessage CreateFallbackPasswordResetEmail(string toEmail, string userName, string resetToken,
        string resetUrl, int expiryMinutes)
    {
        var resetLink = $"{resetUrl}?token={Uri.EscapeDataString(resetToken)}&email={Uri.EscapeDataString(toEmail)}";

        return new EmailMessage
        {
            To = toEmail,
            Subject = "Reset Your Password - FAM",
            HtmlBody = GeneratePasswordResetEmailTemplate(userName, resetLink, expiryMinutes),
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.High,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "password_reset",
                ["userName"] = userName,
                ["fallback"] = "true"
            }
        };
    }

    private EmailMessage CreateFallbackPasswordChangedEmail(string toEmail, string userName)
    {
        return new EmailMessage
        {
            To = toEmail,
            Subject = "Password Changed Successfully - FAM",
            HtmlBody = GeneratePasswordChangedEmailTemplate(userName),
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.Normal,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "password_changed",
                ["userName"] = userName,
                ["fallback"] = "true"
            }
        };
    }

    private EmailMessage CreateFallbackWelcomeEmail(string toEmail, string userName)
    {
        return new EmailMessage
        {
            To = toEmail,
            Subject = "Welcome to FAM - Fixed Asset Management",
            HtmlBody = GenerateWelcomeEmailTemplate(userName),
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.Normal,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "welcome",
                ["userName"] = userName,
                ["fallback"] = "true"
            }
        };
    }

    private EmailMessage CreateFallbackAccountLockedEmail(string toEmail, string userName, string reason)
    {
        return new EmailMessage
        {
            To = toEmail,
            Subject = "Account Security Alert - FAM",
            HtmlBody = GenerateAccountLockedEmailTemplate(userName, reason),
            FromEmail = _fromEmail,
            FromName = _fromName,
            Priority = EmailPriority.Critical,
            Metadata = new Dictionary<string, string>
            {
                ["type"] = "account_locked",
                ["userName"] = userName,
                ["reason"] = reason,
                ["fallback"] = "true"
            }
        };
    }

    #endregion

    #region Hardcoded Email Templates (Fallback)

    private static string GenerateOtpEmailTemplate(string userName, string otpCode)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Verification Code</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">🔐 Verification Code</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p style=""color: #333333; font-size: 16px; line-height: 24px; margin: 0 0 20px 0;"">Hi <strong>{userName}</strong>,</p>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 0 0 30px 0;"">
                                You've requested a verification code to complete your login. Use the code below:
                            </p>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"" style=""background-color: #f8f9fa; padding: 30px; border-radius: 8px; border: 2px dashed #667eea;"">
                                        <div style=""font-size: 36px; font-weight: bold; color: #667eea; letter-spacing: 8px; font-family: 'Courier New', monospace;"">
                                            {otpCode}
                                        </div>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 20px 0;"">
                                This code will <strong>expire in 10 minutes</strong>. Do not share this code with anyone.
                            </p>
                            <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px;"">
                                <p style=""color: #856404; font-size: 13px; margin: 0; line-height: 20px;"">
                                    ⚠️ <strong>Security Notice:</strong> If you didn't request this code, please ignore this email and ensure your account is secure.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-radius: 0 0 8px 8px;"">
                            <p style=""color: #999999; font-size: 12px; margin: 0 0 10px 0;"">
                                This is an automated message from <strong>FAM System</strong>
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                                © {DateTime.UtcNow.Year} Fixed Asset Management. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string GeneratePasswordResetEmailTemplate(string userName, string resetLink, int expiryMinutes)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Reset Your Password</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">🔑 Reset Your Password</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p style=""color: #333333; font-size: 16px; line-height: 24px; margin: 0 0 20px 0;"">Hi <strong>{userName}</strong>,</p>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 0 0 20px 0;"">
                                We received a request to reset your password for your Fixed Asset Management account.
                            </p>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 0 0 30px 0;"">
                                Click the button below to create a new password:
                            </p>
                            <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""margin: 30px 0;"">
                                <tr>
                                    <td align=""center"">
                                        <a href=""{resetLink}"" style=""display: inline-block; padding: 16px 40px; background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: #ffffff; text-decoration: none; border-radius: 6px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 6px rgba(245,87,108,0.3);"">
                                            Reset Password
                                        </a>
                                    </td>
                                </tr>
                            </table>
                            <p style=""color: #666666; font-size: 13px; line-height: 20px; margin: 20px 0;"">
                                Or copy and paste this link into your browser:
                            </p>
                            <div style=""background-color: #f8f9fa; padding: 15px; border-radius: 4px; word-break: break-all; margin: 10px 0 20px 0;"">
                                <p style=""color: #667eea; font-size: 12px; margin: 0; font-family: monospace;"">{resetLink}</p>
                            </div>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 20px 0;"">
                                This link will <strong>expire in {expiryMinutes} minutes</strong> for security reasons.
                            </p>
                            <div style=""background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 15px; margin: 20px 0; border-radius: 4px;"">
                                <p style=""color: #721c24; font-size: 13px; margin: 0; line-height: 20px;"">
                                    🚨 <strong>Important:</strong> If you didn't request a password reset, please ignore this email.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-radius: 0 0 8px 8px;"">
                            <p style=""color: #999999; font-size: 12px; margin: 0 0 10px 0;"">
                                This is an automated message from <strong>FAM System</strong>
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                                © {DateTime.UtcNow.Year} Fixed Asset Management. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string GeneratePasswordChangedEmailTemplate(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Changed</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">✅ Password Changed Successfully</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p style=""color: #333333; font-size: 16px; line-height: 24px; margin: 0 0 20px 0;"">Hi <strong>{userName}</strong>,</p>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 0 0 20px 0;"">
                                This is a confirmation that your password has been changed successfully.
                            </p>
                            <div style=""background-color: #d4edda; border-left: 4px solid #28a745; padding: 20px; margin: 20px 0; border-radius: 4px;"">
                                <p style=""color: #155724; font-size: 14px; margin: 0 0 10px 0; font-weight: 600;"">
                                    ✓ Your password was successfully updated
                                </p>
                                <p style=""color: #155724; font-size: 13px; margin: 0;"">
                                    Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                                </p>
                            </div>
                            <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px;"">
                                <p style=""color: #856404; font-size: 13px; margin: 0; line-height: 20px;"">
                                    ⚠️ <strong>Did not make this change?</strong> Please contact support immediately.
                                </p>
                            </div>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-radius: 0 0 8px 8px;"">
                            <p style=""color: #999999; font-size: 12px; margin: 0 0 10px 0;"">
                                This is an automated message from <strong>FAM System</strong>
                            </p>
                            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                                © {DateTime.UtcNow.Year} Fixed Asset Management. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string GenerateWelcomeEmailTemplate(string userName)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Welcome to FAM</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">🎉 Welcome to FAM!</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p style=""color: #333333; font-size: 16px; line-height: 24px; margin: 0 0 20px 0;"">Hi <strong>{userName}</strong>,</p>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 0 0 20px 0;"">
                                Welcome to Fixed Asset Management System! Your account has been created successfully.
                            </p>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 0 0 20px 0;"">
                                You can now log in and start managing your organization's assets efficiently.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-radius: 0 0 8px 8px;"">
                            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                                © {DateTime.UtcNow.Year} Fixed Asset Management. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    private static string GenerateAccountLockedEmailTemplate(string userName, string reason)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Account Security Alert</title>
</head>
<body style=""margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;"">
    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #f4f4f4; padding: 20px;"">
        <tr>
            <td align=""center"">
                <table width=""600"" cellpadding=""0"" cellspacing=""0"" style=""background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
                    <tr>
                        <td style=""background: linear-gradient(135deg, #dc3545 0%, #c82333 100%); padding: 40px 30px; text-align: center; border-radius: 8px 8px 0 0;"">
                            <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: 600;"">🚨 Account Locked</h1>
                        </td>
                    </tr>
                    <tr>
                        <td style=""padding: 40px 30px;"">
                            <p style=""color: #333333; font-size: 16px; line-height: 24px; margin: 0 0 20px 0;"">Hi <strong>{userName}</strong>,</p>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 0 0 20px 0;"">
                                Your account has been temporarily locked for security reasons.
                            </p>
                            <div style=""background-color: #f8d7da; border-left: 4px solid #dc3545; padding: 20px; margin: 20px 0; border-radius: 4px;"">
                                <p style=""color: #721c24; font-size: 14px; margin: 0 0 10px 0; font-weight: 600;"">
                                    Reason: {reason}
                                </p>
                                <p style=""color: #721c24; font-size: 13px; margin: 0;"">
                                    Time: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                                </p>
                            </div>
                            <p style=""color: #666666; font-size: 14px; line-height: 22px; margin: 20px 0;"">
                                If this was not you, please contact support immediately.
                            </p>
                        </td>
                    </tr>
                    <tr>
                        <td style=""background-color: #f8f9fa; padding: 30px; text-align: center; border-radius: 0 0 8px 8px;"">
                            <p style=""color: #999999; font-size: 12px; margin: 0;"">
                                © {DateTime.UtcNow.Year} Fixed Asset Management. All rights reserved.
                            </p>
                        </td>
                    </tr>
                </table>
            </td>
        </tr>
    </table>
</body>
</html>";
    }

    #endregion
}
