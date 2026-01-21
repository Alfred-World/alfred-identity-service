using System.ComponentModel.DataAnnotations;

namespace Alfred.Identity.WebApi.Contracts.Auth;

/// <summary>
/// Request model for SSO login (sets authentication cookie)
/// </summary>
public sealed record SsoLoginRequest
{
    /// <summary>
    /// User identity - can be email or username
    /// </summary>
    [Required]
    [StringLength(256)]
    public required string Identity { get; init; }

    /// <summary>
    /// User password
    /// </summary>
    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    /// <summary>
    /// URL to redirect after successful login (OIDC flow)
    /// </summary>
    public string? ReturnUrl { get; init; }

    /// <summary>
    /// Remember me - extends cookie validity
    /// </summary>
    public bool RememberMe { get; init; } = false;
}

