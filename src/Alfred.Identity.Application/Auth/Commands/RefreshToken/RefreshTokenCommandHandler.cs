using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Common.Enums;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand - implements token rotation.
/// </summary>
public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IApplicationRepository _applicationRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILocationService _locationService;
    private readonly ICacheProvider _cacheProvider;

    private const int RefreshTokenLifetimeSeconds = 604800;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IApplicationRepository applicationRepository,
        ITokenRepository tokenRepository,
        IJwtTokenService jwtTokenService,
        ILocationService locationService,
        ICacheProvider cacheProvider)
    {
        _userRepository = userRepository;
        _applicationRepository = applicationRepository;
        _tokenRepository = tokenRepository;
        _jwtTokenService = jwtTokenService;
        _locationService = locationService;
        _cacheProvider = cacheProvider;
    }

    public async Task<RefreshTokenResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
        var storedToken = await _tokenRepository.GetByReferenceIdAsync(tokenHash, cancellationToken);
        if (storedToken == null)
        {
            return new RefreshTokenResult(false, Error: "Invalid refresh token");
        }

        if (storedToken.Status == TokenStatus.Redeemed || storedToken.RedemptionDate.HasValue)
        {
            if (storedToken.UserId.HasValue)
            {
                await _tokenRepository.RevokeAllByUserIdAsync(storedToken.UserId.Value, cancellationToken);
                await _tokenRepository.SaveChangesAsync(cancellationToken);
            }

            return new RefreshTokenResult(false, Error: "Token has been reused - all sessions revoked");
        }

        if (storedToken.Status != TokenStatus.Valid ||
            (storedToken.ExpirationDate.HasValue && DateTime.UtcNow > storedToken.ExpirationDate.Value))
        {
            return new RefreshTokenResult(false, Error: "Refresh token is invalid or expired");
        }

        if (await _cacheProvider.ExistsAsync($"session:revoked:{storedToken.Id}", cancellationToken))
        {
            return new RefreshTokenResult(false, Error: "Session has been revoked");
        }

        if (storedToken.AuthorizationId.HasValue &&
            await _cacheProvider.ExistsAsync($"revoked:session:{storedToken.AuthorizationId.Value}", cancellationToken))
        {
            return new RefreshTokenResult(false, Error: "Session has been revoked");
        }

        if (!storedToken.UserId.HasValue || !storedToken.ApplicationId.HasValue)
        {
            return new RefreshTokenResult(false, Error: "Invalid token state");
        }

        var user = await _userRepository.GetByIdAsync(storedToken.UserId.Value, cancellationToken);
        if (user == null || !user.CanLogin())
        {
            return new RefreshTokenResult(false, Error: "User account is not active");
        }

        var application = await _applicationRepository.GetByIdAsync(storedToken.ApplicationId.Value, cancellationToken);
        if (application is not { IsActive: true })
        {
            return new RefreshTokenResult(false, Error: "Client application is inactive");
        }

        await _tokenRepository.RedeemByIdAsync(storedToken.Id, cancellationToken);

        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
            user.Id.Value,
            user.Email,
            user.FullName,
            application.Id.Value,
            application.ClientId,
            storedToken.AuthorizationId?.Value);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var location = request.IpAddress != null
            ? await _locationService.GetLocationFromIpAsync(request.IpAddress)
            : null;

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

        try
        {
            await _tokenRepository.DeleteExpiredAndRedeemedByUserAsync(user.Id, CancellationToken.None);
        }
        catch
        {
            /* non-critical cleanup */
        }

        return new RefreshTokenResult(
            true,
            accessToken,
            newRefreshTokenValue,
            _jwtTokenService.AccessTokenLifetimeSeconds
        );
    }
}
