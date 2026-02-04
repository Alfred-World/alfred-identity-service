using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IEmailSender _emailSender;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        ITokenRepository tokenRepository,
        IEmailSender emailSender)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailSender = emailSender;
    }

    public async Task<Result<bool>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (user == null)
        {
            // Don't reveal that the user does not exist
            return Result<bool>.Success(true);
        }

        // Generate Reset Token
        var resetToken = Guid.NewGuid().ToString("N");
        var tokenEntity = Token.Create(
            OAuthConstants.TokenTypes.PasswordReset,
            null,
            user.Id.ToString(),
            user.Id,
            DateTime.UtcNow.AddMinutes(15), // 15 minutes expiry
            resetToken,
            null,
            null,
            null,
            null,
            null,
            null
        );

        await _tokenRepository.AddAsync(tokenEntity, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // Send Email
        var resetLink = $"https://your-app.com/reset-password?token={resetToken}&email={user.Email}";
        await _emailSender.SendEmailAsync(
            to: user.Email,
            subject: "Reset Your Password",
            htmlBody: string.Empty,
            templateCode: "forgot_password",
            templateParams: new { fullName = user.FullName, resetLink },
            cancellationToken: cancellationToken);

        return Result<bool>.Success(true);
    }
}
