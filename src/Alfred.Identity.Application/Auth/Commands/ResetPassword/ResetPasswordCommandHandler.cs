using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Common.Enums;
using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository, 
        ITokenRepository tokenRepository, 
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // validate token
        var token = await _tokenRepository.GetByReferenceIdAsync(request.Token, cancellationToken);
        if (token == null || token.Type != OAuthConstants.TokenTypes.PasswordReset)
        {
            return Result<bool>.Failure("Invalid token");
        }

        if (token.Status != TokenStatus.Valid || (token.ExpirationDate.HasValue && token.ExpirationDate < DateTime.UtcNow))
        {
            return Result<bool>.Failure("Token expired or already used");
        }

        // Validate user match
        var user = await _userRepository.GetByIdAsync(token.UserId!.Value, cancellationToken);
        if (user == null || !user.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
        {
            return Result<bool>.Failure("Invalid request");
        }

        // Reset Password
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.SetPassword(newPasswordHash);

        // Redeem Token
        token.Redeem();

        await _userRepository.SaveChangesAsync(cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
