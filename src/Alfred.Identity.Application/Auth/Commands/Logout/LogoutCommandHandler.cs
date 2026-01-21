using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Logout;

/// <summary>
/// Handler for LogoutCommand
/// </summary>
public class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResult>
{
    private readonly ITokenRepository _tokenRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public LogoutCommandHandler(
        ITokenRepository tokenRepository,
        IJwtTokenService jwtTokenService)
    {
        _tokenRepository = tokenRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<LogoutResult> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // Hash the refresh token
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);

        // Find and revoke the token
        var storedToken = await _tokenRepository.GetByReferenceIdAsync(tokenHash, cancellationToken);
        if (storedToken == null)
        {
            // Token not found - could be already logged out or invalid
            // Return success anyway (idempotent)
            return new LogoutResult(true);
        }

        // Revoke the token
        storedToken.Revoke();
        _tokenRepository.Update(storedToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return new LogoutResult(true);
    }
}
