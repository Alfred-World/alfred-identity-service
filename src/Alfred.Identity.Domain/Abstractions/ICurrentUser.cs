using System.Security.Claims;

namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Provides access to the current authenticated user's information
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Gets the current user's ID, or null if not authenticated
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// Gets the current user's username, or null if not authenticated
    /// </summary>
    string? Username { get; }

    /// <summary>
    /// Gets the current user's email, or null if not authenticated
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets whether the current user is authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the current user's claims principal
    /// </summary>
    ClaimsPrincipal? Principal { get; }

    /// <summary>
    /// Gets the current user's ID, throwing if not authenticated
    /// </summary>
    /// <exception cref="UnauthorizedAccessException">If user is not authenticated</exception>
    Guid GetRequiredUserId();
}
