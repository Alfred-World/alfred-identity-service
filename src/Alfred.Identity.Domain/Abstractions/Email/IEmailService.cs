namespace Alfred.Identity.Domain.Abstractions.Email;

/// <summary>
/// Service để gửi email
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Gửi email OTP cho xác thực 2FA
    /// </summary>
    Task SendOtpEmailAsync(string toEmail, string otpCode, string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi email reset password
    /// </summary>
    /// <param name="expiryMinutes">Thời gian hết hạn của reset token (phút)</param>
    Task SendPasswordResetEmailAsync(string toEmail, string resetToken, string userName, string resetUrl,
        int expiryMinutes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gửi email xác nhận đổi password thành công
    /// </summary>
    Task SendPasswordChangedEmailAsync(string toEmail, string userName, CancellationToken cancellationToken = default);
}
