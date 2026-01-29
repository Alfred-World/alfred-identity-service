using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents an OAuth2 application, aligned with OpenIddictApplications schema
/// </summary>
public class Application : BaseEntity
{
    public string ClientId { get; private set; } = null!;
    public string? ClientSecret { get; private set; } // Hashed
    public string? DisplayName { get; private set; }
    public string? DisplayNames { get; private set; } // JSON
    public string? Permissions { get; private set; } // JSON or space delimited
    public string? RedirectUris { get; private set; } // JSON array or Space delimited
    public string? PostLogoutRedirectUris { get; private set; }
    public string? ApplicationType { get; private set; } = "web"; // web/native/server
    public string? ClientType { get; private set; } = "confidential"; // public/confidential
    public string? ConsentType { get; private set; } = "explicit"; // explicit/implicit/system
    public string? Requirements { get; private set; } // JSON
    public string? Settings { get; private set; } // JSON
    public string? JsonWebKeySet { get; private set; }
    public string? ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString();

    // Mapping custom fields
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Application()
    {
    }

    public static Application Create(
        string clientId,
        string displayName,
        string? clientSecret = null,
        string? redirectUris = null,
        string? postLogoutRedirectUris = null,
        string? permissions = null,
        string clientType = "confidential",
        string applicationType = "web")
    {
        return new Application
        {
            ClientId = clientId,
            DisplayName = displayName,
            ClientSecret = clientSecret,
            RedirectUris = redirectUris,
            PostLogoutRedirectUris = postLogoutRedirectUris,
            Permissions = permissions,
            ClientType = clientType,
            ApplicationType = applicationType,
            ConcurrencyToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
    }

    public void Update(
        string? displayName,
        string? redirectUris,
        string? postLogoutRedirectUris,
        string? permissions,
        string? clientType)
    {
        DisplayName = displayName;
        RedirectUris = redirectUris;
        PostLogoutRedirectUris = postLogoutRedirectUris;
        Permissions = permissions;
        ClientType = clientType;
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRedirectUris(string redirectUris)
    {
        RedirectUris = redirectUris;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RotateSecret(string newSecretHash)
    {
        ClientSecret = newSecretHash;
        ConcurrencyToken = Guid.NewGuid().ToString();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
