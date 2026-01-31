using Alfred.Identity.Application.Auth.Commands.Login;

namespace Alfred.Identity.WebApi.Contracts.Auth;

/// <summary>
/// Response for SSO Login
/// </summary>
/// <remarks>
/// Note: AccessToken/RefreshToken are intentionally NOT included here.
/// SSO flow uses cookie-based authentication. Client only needs the exchange URL.
/// </remarks>
public sealed record SsoLoginResponse
{
    /// <summary>
    /// Exchange URL for browser navigation to set cookie (Token Exchange Pattern)
    /// </summary>
    /// <remarks>
    /// Client should navigate to this URL: window.location.href = response.returnUrl
    /// </remarks>
    public required string ReturnUrl { get; init; }

    /// <summary>
    /// User information for UI display
    /// </summary>
    public UserInfo User { get; init; } = null!;
}

/// <summary>
/// Response for SSO Session check
/// </summary>
public sealed record SsoSessionResponse
{
    /// <summary>
    /// Whether user is authenticated
    /// </summary>
    public bool IsAuthenticated { get; init; }

    /// <summary>
    /// User info if authenticated
    /// </summary>
    public SessionUserInfoDto? User { get; init; }
}

/// <summary>
/// User info for session response
/// </summary>
public sealed record SessionUserInfoDto
{
    /// <summary>
    /// User ID (Guid - matches DB schema)
    /// </summary>
    public Guid Id { get; init; }

    public string Email { get; init; } = "";
    public string? FullName { get; init; }
    public string? UserName { get; init; }
}
