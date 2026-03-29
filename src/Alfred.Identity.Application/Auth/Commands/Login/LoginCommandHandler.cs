using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Common.Constants;
using Alfred.Identity.Domain.Entities;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Login;

/// <summary>
/// Handler for LoginCommand
/// </summary>
public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginData>>
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenRepository _tokenRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ILocationService _locationService;


    public LoginCommandHandler(
        IUserRepository userRepository,
        ITokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILocationService locationService)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _locationService = locationService;
    }

    public async Task<Result<LoginData>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // Find user by identity (email or username)
        var user = await _userRepository.GetByIdentityAsync(request.Identity, cancellationToken);
        if (user == null)
        {
            return Result<LoginData>.Failure("Invalid credentials");
        }

        // Check if user can login
        if (!user.CanLogin())
        {
            return Result<LoginData>.Failure("Account is not active");
        }

        // SSO login requires verified email.
        if (request.IsSsoFlow && !user.EmailConfirmed)
        {
            return Result<LoginData>.Failure("Email is not confirmed");
        }

        // Verify password
        if (!user.HasPassword() || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash!))
        {
            return Result<LoginData>.Failure("Invalid credentials");
        }

        // Get location from IP
        var location = request.IpAddress != null
            ? await _locationService.GetLocationFromIpAsync(request.IpAddress)
            : null;

        string accessToken;
        string refreshTokenValue;

        if (request.IsSsoFlow)
        {
            // SSO web flow: credentials are validated here only.
            // The OIDC /connect/token endpoint issues tokens after code exchange.
            // Creating tokens here produces orphaned rows (ApplicationId=NULL, never delivered to any client).
            // Clean up stale tokens left over from previous SSO attempts for this user instead.
            await _tokenRepository.DeleteExpiredAndRedeemedByUserAsync(user.Id, cancellationToken);
            await _tokenRepository.SaveChangesAsync(cancellationToken);
            accessToken = string.Empty;
            refreshTokenValue = string.Empty;
        }
        else
        {
            // Direct API login (non-OIDC clients): generate and persist tokens.
            accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user.Id.Value, user.Email, user.FullName);
            refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshTokenValue);
            var ip = request.IpAddress ?? "Unknown";
            var device = request.DeviceName ?? "Unknown";
            var loc = location ?? "Unknown";
            var properties = "{" + $"\"ip\": \"{ip}\", \"device\": \"{device}\", \"location\": \"{loc}\"" + "}";

            var refreshToken = Token.Create(
                OAuthConstants.TokenTypes.RefreshToken,
                null,
                user.Id.ToString(),
                user.Id,
                DateTime.UtcNow.AddSeconds(_jwtTokenService.RefreshTokenLifetimeSeconds),
                refreshTokenHash,
                null,
                null,
                properties,
                request.IpAddress,
                location,
                request.DeviceName
            );

            await _tokenRepository.AddAsync(refreshToken, cancellationToken);
            await _tokenRepository.SaveChangesAsync(cancellationToken);
        }

        // Return login data with user info
        var loginData = new LoginData
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = _jwtTokenService.AccessTokenLifetimeSeconds,
            TokenType = "Bearer",
            User = new UserInfo
            {
                Id = user.Id.Value,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName
            }
        };

        return Result<LoginData>.Success(loginData);
    }
}
