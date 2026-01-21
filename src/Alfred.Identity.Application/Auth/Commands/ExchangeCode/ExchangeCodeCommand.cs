using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.ExchangeCode;

/// <summary>
/// Command to exchange an authorization code for access/refresh tokens
/// </summary>
public record ExchangeCodeCommand(
    string GrantType,
    string? ClientId,
    string? ClientSecret,
    string? Code,
    string? RedirectUri,
    string? CodeVerifier,
    string? RefreshToken // For refresh_token grant type
) : IRequest<ExchangeCodeResult>;

/// <summary>
/// Result of token exchange
/// </summary>
public record ExchangeCodeResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    string? IdToken = null,
    string? TokenType = "Bearer",
    int ExpiresIn = 0,
    string? Error = null,
    string? ErrorDescription = null
);
