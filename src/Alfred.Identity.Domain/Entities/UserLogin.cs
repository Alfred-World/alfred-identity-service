using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a login from an external provider (Google, Facebook, etc.)
/// Linked to a specific User
/// </summary>
public class UserLogin : BaseEntity
{
    // Composite Key -> Now Unique Index
    public string LoginProvider { get; private set; } = null!; // e.g., "Google"

    public string ProviderKey { get; private set; } = null!; // e.g., "1234567890" (Google User ID)

    public string? ProviderDisplayName { get; private set; } // e.g., "Van Anh"

    public Guid UserId { get; private set; }
    public virtual User User { get; private set; } = null!;

    private UserLogin()
    {
    }

    public static UserLogin Create(string loginProvider, string providerKey, string? providerDisplayName, Guid userId)
    {
        return new UserLogin
        {
            Id = Guid.NewGuid(),
            LoginProvider = loginProvider,

            ProviderKey = providerKey,
            ProviderDisplayName = providerDisplayName,
            UserId = userId
        };
    }
}
