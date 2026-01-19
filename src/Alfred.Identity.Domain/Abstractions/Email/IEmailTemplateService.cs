namespace Alfred.Identity.Domain.Abstractions.Email;

/// <summary>
/// Email template service - generates HTML email bodies
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Generate OTP verification email
    /// </summary>
    EmailMessage CreateOtpEmail(string toEmail, string userName, string otpCode);

    /// <summary>
    /// Generate password reset email
    /// </summary>
    /// <param name="expiryMinutes">Thời gian hết hạn của reset token (phút)</param>
    EmailMessage CreatePasswordResetEmail(string toEmail, string userName, string resetToken, string resetUrl,
        int expiryMinutes);

    /// <summary>
    /// Generate password changed confirmation email
    /// </summary>
    EmailMessage CreatePasswordChangedEmail(string toEmail, string userName);

    /// <summary>
    /// Generate welcome email for new user
    /// </summary>
    EmailMessage CreateWelcomeEmail(string toEmail, string userName);

    /// <summary>
    /// Generate account locked notification email
    /// </summary>
    EmailMessage CreateAccountLockedEmail(string toEmail, string userName, string reason);
}
