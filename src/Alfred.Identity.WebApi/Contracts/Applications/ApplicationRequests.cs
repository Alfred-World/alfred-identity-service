using System.ComponentModel.DataAnnotations;

namespace Alfred.Identity.WebApi.Contracts.Applications;

/// <summary>
/// Request model for creating a new OAuth2 client application
/// </summary>
public sealed record CreateApplicationRequest
{
    /// <summary>
    /// Unique client identifier
    /// </summary>
    [Required]
    [StringLength(100)]
    public required string ClientId { get; init; }

    /// <summary>
    /// Display name for the application
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Comma-separated list of allowed redirect URIs
    /// </summary>
    [Required]
    public required string RedirectUris { get; init; }

    /// <summary>
    /// Comma-separated list of allowed post-logout redirect URIs
    /// </summary>
    public string? PostLogoutRedirectUris { get; init; }

    /// <summary>
    /// Comma-separated list of permissions/scopes
    /// </summary>
    public string? Permissions { get; init; }

    /// <summary>
    /// Application type: public or confidential
    /// </summary>
    [RegularExpression("^(public|confidential)$", ErrorMessage = "Type must be 'public' or 'confidential'")]
    public string Type { get; init; } = "public";
}

/// <summary>
/// Request model for updating an OAuth2 client application
/// </summary>
public sealed record UpdateApplicationRequest
{
    /// <summary>
    /// Display name for the application
    /// </summary>
    [Required]
    [StringLength(200)]
    public required string DisplayName { get; init; }

    /// <summary>
    /// Comma-separated list of allowed redirect URIs
    /// </summary>
    [Required]
    public required string RedirectUris { get; init; }

    /// <summary>
    /// Comma-separated list of allowed post-logout redirect URIs
    /// </summary>
    public string? PostLogoutRedirectUris { get; init; }

    /// <summary>
    /// Comma-separated list of permissions/scopes
    /// </summary>
    public string? Permissions { get; init; }
}

public record UpdateApplicationStatusRequest(
    bool IsActive
);
