using Alfred.Identity.Application.Auth.Common;
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
    private readonly IApplicationRepository _applicationRepository;
    private readonly IClientSecretHasher _clientSecretHasher;

    public LoginCommandHandler(
        IUserRepository userRepository,
        ITokenRepository tokenRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        ILocationService locationService,
        IApplicationRepository applicationRepository,
        IClientSecretHasher clientSecretHasher)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _locationService = locationService;
        _applicationRepository = applicationRepository;
        _clientSecretHasher = clientSecretHasher;
    }

    public async Task<Result<LoginData>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdentityAsync(request.Identity, cancellationToken);
        if (user == null)
        {
            return Result<LoginData>.Failure("Invalid credentials");
        }

        if (!user.CanLogin())
        {
            return Result<LoginData>.Failure("Account is not active");
        }

        if (request.IsSsoFlow && !user.EmailConfirmed)
        {
            return Result<LoginData>.Failure("Email is not confirmed");
        }

        if (!user.HasPassword() || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash!))
        {
            return Result<LoginData>.Failure("Invalid credentials");
        }

        var location = request.IpAddress != null
            ? await _locationService.GetLocationFromIpAsync(request.IpAddress)
            : null;

        string accessToken;
        string refreshTokenValue;

        if (request.IsSsoFlow)
        {
            await _tokenRepository.DeleteExpiredAndRedeemedByUserAsync(user.Id, cancellationToken);
            await _tokenRepository.SaveChangesAsync(cancellationToken);
            accessToken = string.Empty;
            refreshTokenValue = string.Empty;
        }
        else
        {
            var clientResult = await ValidateDirectLoginClientAsync(request, cancellationToken);
            if (clientResult.IsFailure)
            {
                return Result<LoginData>.Failure(clientResult.Error!);
            }

            var client = clientResult.Value!;
            accessToken = await _jwtTokenService.GenerateAccessTokenAsync(
                user.Id.Value,
                user.Email,
                user.FullName,
                client.Id.Value,
                client.ClientId);
            refreshTokenValue = _jwtTokenService.GenerateRefreshToken();

            var refreshTokenHash = _jwtTokenService.HashRefreshToken(refreshTokenValue);
            var ip = request.IpAddress ?? "Unknown";
            var device = request.DeviceName ?? "Unknown";
            var loc = location ?? "Unknown";
            var properties = "{" + $"\"ip\": \"{ip}\", \"device\": \"{device}\", \"location\": \"{loc}\"" + "}";

            var refreshToken = Token.Create(
                OAuthConstants.TokenTypes.RefreshToken,
                client.Id,
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

    private async Task<Result<Domain.Entities.Application>> ValidateDirectLoginClientAsync(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            return Result<Domain.Entities.Application>.Failure("Client ID is required");
        }

        var client = await _applicationRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client is not { IsActive: true })
        {
            return Result<Domain.Entities.Application>.Failure("Invalid client");
        }

        if (!OidcClientPermissions.SupportsEndpoint(client, ApplicationConstants.Endpoints.Token))
        {
            return Result<Domain.Entities.Application>.Failure("Client is not allowed to use the token endpoint");
        }

        if (!OidcClientPermissions.SupportsGrantType(client, OAuthConstants.GrantTypes.Password))
        {
            return Result<Domain.Entities.Application>.Failure("Client is not allowed to use direct password login");
        }

        if (client.ClientType?.Equals(ApplicationConstants.ClientTypes.Confidential,
                StringComparison.OrdinalIgnoreCase) == true)
        {
            if (string.IsNullOrWhiteSpace(request.ClientSecret) || string.IsNullOrWhiteSpace(client.ClientSecret))
            {
                return Result<Domain.Entities.Application>.Failure(
                    "Client secret is required for confidential clients");
            }

            if (!_clientSecretHasher.VerifySecret(request.ClientSecret, client.ClientSecret))
            {
                return Result<Domain.Entities.Application>.Failure("Invalid client secret");
            }
        }

        return Result<Domain.Entities.Application>.Success(client);
    }
}
