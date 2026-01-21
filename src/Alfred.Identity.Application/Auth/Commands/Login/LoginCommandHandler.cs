using Alfred.Identity.Application.Common;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Abstractions.Services;
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

    private const int AccessTokenLifetimeSeconds = 900; // 15 minutes
    private const int RefreshTokenLifetimeSeconds = 604800; // 7 days

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

        // Verify password
        if (!user.HasPassword() || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash!))
        {
            return Result<LoginData>.Failure("Invalid credentials");
        }

        // Generate tokens
        var accessToken = await _jwtTokenService.GenerateAccessTokenAsync(user.Id, user.Email, user.FullName);
        var refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        // Get location from IP
        var location = request.IpAddress != null
            ? await _locationService.GetLocationFromIpAsync(request.IpAddress)
            : null;

        // Create and store new refresh token as a Token entity
        var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshTokenValue);

        // Properties JSON (simplified for now)
        var properties = location != null
            ? $"{{\"location\": \"{location}\", \"device\": \"{request.DeviceName ?? "Unknown"}\", \"ip\": \"{request.IpAddress}\"}}"
            : null;

        var refreshToken = Token.Create(
            "refresh_token",
            null,
            user.Id.ToString(),
            user.Id,
            DateTime.UtcNow.AddSeconds(RefreshTokenLifetimeSeconds),
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

        // Return login data with user info
        var loginData = new LoginData
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = AccessTokenLifetimeSeconds,
            TokenType = "Bearer",
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                FullName = user.FullName
            }
        };

        return Result<LoginData>.Success(loginData);
    }
}
