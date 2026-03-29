namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a login from an external provider (Google, Facebook, etc.)
/// Linked to a specific User
/// </summary>
public sealed class UserLogin : BaseEntity<UserLoginId>
{
    // Composite Key -> Now Unique Index
    public string LoginProvider { get; private set; } = null!; // e.g., "Google"

    public string ProviderKey { get; private set; } = null!; // e.g., "1234567890" (Google User ID)

    public string? ProviderDisplayName { get; private set; } // e.g., "Van Anh"

    public UserId UserId { get; private set; }
    public User User { get; private set; } = null!;

    private UserLogin()
    {
        Id = UserLoginId.New();
    }

    public static UserLogin Create(string loginProvider, string providerKey, string? providerDisplayName, UserId userId)
    {
        return new UserLogin
        {
            Id = UserLoginId.New(),
            LoginProvider = loginProvider,

            ProviderKey = providerKey,
            ProviderDisplayName = providerDisplayName,
            UserId = userId
        };
    }
}
