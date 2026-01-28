using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Common.Enums;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand - implements token rotation
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILocationService _locationService;

    private const int RefreshTokenLifetimeSeconds = 604800; // 7 days

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        ITokenRepository tokenRepository,
        IJwtTokenService jwtTokenService,
        ILocationService locationService)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _jwtTokenService = jwtTokenService;
        _locationService = locationService;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // Hash the incoming refresh token
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);

        // Find the refresh token
        var storedToken = await _tokenRepository.GetByReferenceIdAsync(tokenHash, cancellationToken);
        if (storedToken == null)
        {
            return new RefreshTokenResult(false, Error: "Invalid refresh token");
        }

        // Check if token is already used (potential token reuse attack)
        if (storedToken.Status == TokenStatus.Redeemed || storedToken.RedemptionDate.HasValue)
        {
            // Revoke all tokens for this user (security measure)
            if (storedToken.UserId.HasValue)
            {
                await _tokenRepository.RevokeAllByUserIdAsync(storedToken.UserId.Value, cancellationToken);
                await _tokenRepository.SaveChangesAsync(cancellationToken);
            }

            return new RefreshTokenResult(false, Error: "Token has been reused - all sessions revoked");
        }

        // Check if token is revoked or expired
        if (storedToken.Status != TokenStatus.Valid ||
            (storedToken.ExpirationDate.HasValue && DateTime.UtcNow > storedToken.ExpirationDate.Value))
        {
            return new RefreshTokenResult(false, Error: "Refresh token is invalid or expired");
        }

        // Get user
        if (!storedToken.UserId.HasValue)
        {
            return new RefreshTokenResult(false, Error: "Invalid token state");
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId.Value, cancellationToken);
        if (user == null || !user.CanLogin())
        {
            return new RefreshTokenResult(false, Error: "User account is not active");
        }

        // Mark old token as used (token rotation)
        storedToken.Redeem();
        _tokenRepository.Update(storedToken);

        // Generate new tokens
        var accessToken =
            await _jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.FullName,
                storedToken.ApplicationId);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();
        var jwtId = _jwtTokenService.GetJwtIdFromToken(accessToken);

        // Get location from IP
        var location = request.IpAddress != null
            ? await _locationService.GetLocationFromIpAsync(request.IpAddress)
            : null;

        // Create and store new refresh token
        var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshTokenValue);

        var properties = location != null
            ? $"{{\"location\": \"{location}\", \"device\": \"{request.DeviceName ?? "Unknown"}\", \"ip\": \"{request.IpAddress}\"}}"
            : null;

        var newRefreshToken = Token.Create(
            OAuthConstants.TokenTypes.RefreshToken,
            storedToken.ApplicationId,
            user.Id.ToString(),
            user.Id,
            DateTime.UtcNow.AddSeconds(RefreshTokenLifetimeSeconds),
            newRefreshTokenHash,
            storedToken.AuthorizationId,
            null,
            properties,
            request.IpAddress,
            location,
            request.DeviceName
        );

        await _tokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResult(
            true,
            accessToken,
            newRefreshTokenValue,
            900 // 15 minutes
        );
    }
}
