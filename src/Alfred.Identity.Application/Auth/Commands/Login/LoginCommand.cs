using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Auth.Commands.Login;

/// <summary>
/// Command to login a user
/// </summary>
public record LoginCommand(
    string Identity,
    string Password,
    bool RememberMe = false,
    string? IpAddress = null,
    string? DeviceName = null,
    /// <summary>
    /// When true (SSO web flow), skip direct refresh-token creation.
    /// The OIDC flow issues its own tokens via /connect/token — creating tokens here would only add orphaned rows.
    /// </summary>
    bool IsSsoFlow = false
) : IRequest<Result<LoginData>>;

/// <summary>
/// Login response data - contains tokens and user info
/// </summary>
public record LoginData
{
    /// <summary>
    /// JWT Access token
    /// </summary>
    public string AccessToken { get; init; } = null!;

    /// <summary>
    /// Refresh token for obtaining new access tokens
    /// </summary>
    public string RefreshToken { get; init; } = null!;

    /// <summary>
    /// Access token expiration time in seconds
    /// </summary>
    public int ExpiresIn { get; init; }

    /// <summary>
    /// Token type (always "Bearer")
    /// </summary>
    public string TokenType { get; init; } = "Bearer";

    /// <summary>
    /// User information
    /// </summary>
    public UserInfo User { get; init; } = null!;
}

/// <summary>
/// Basic user information returned with login
/// </summary>
public record UserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = null!;
    public string? UserName { get; init; }
    public string? FullName { get; init; }
}
