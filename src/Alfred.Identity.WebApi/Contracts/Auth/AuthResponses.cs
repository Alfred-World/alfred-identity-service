using Alfred.Identity.Application.Auth.Commands.Login;

namespace Alfred.Identity.WebApi.Contracts.Auth;

/// <summary>
/// Response for SSO Login
/// </summary>
public sealed record SsoLoginResponse
{
    /// <summary>
    /// Exchange URL for browser navigation to set cookie (Token Exchange Pattern)
    /// </summary>
    public string? ReturnUrl { get; init; }
    
    /// <summary>
    /// User information
    /// </summary>
    public UserInfo User { get; init; } = null!;
    
    /// <summary>
    /// One-time exchange token for cookie authentication (Token Exchange Pattern)
    /// </summary>
    public string? ExchangeToken { get; init; }
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
    public Guid Id { get; init; }
    public string Email { get; init; } = "";
    public string? FullName { get; init; }
    public string? UserName { get; init; }
}

