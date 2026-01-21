using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh access token
/// </summary>
public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? DeviceName = null
) : IRequest<RefreshTokenResult>;

/// <summary>
/// Result of token refresh
/// </summary>
public record RefreshTokenResult(
    bool Success,
    string? AccessToken = null,
    string? RefreshToken = null,
    int? ExpiresIn = null,
    string? Error = null
);
