using Alfred.Identity.Domain.ValueObjects;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents an OAuth2 application, aligned with OpenIddictApplications schema
/// </summary>
public sealed class Application : BaseEntity<ApplicationId>, IHasCreationTime, IHasCreator, IHasModificationTime,
    IHasModifier
{
    public string ClientId { get; private set; } = null!;
    public string? ClientSecret { get; private set; } // Hashed
    public string? DisplayName { get; private set; }
    public string? DisplayNames { get; private set; } // JSON
    public string? Permissions { get; private set; } // JSON or space delimited
    public RedirectUriCollection RedirectUris { get; private set; } = RedirectUriCollection.Empty();
    public RedirectUriCollection PostLogoutRedirectUris { get; private set; } = RedirectUriCollection.Empty();
    public string? ApplicationType { get; private set; } = "web"; // web/native/server
    public string? ClientType { get; private set; } = "confidential"; // public/confidential
    public string? ConsentType { get; private set; } = "explicit"; // explicit/implicit/system
    public string? Requirements { get; private set; } // JSON
    public string? Settings { get; private set; } // JSON
    public string? JsonWebKeySet { get; private set; }
    public string? ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString();

    // Mapping custom fields
    public bool IsActive { get; private set; } = true;

    // Audit fields
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    private Application()
    {
        Id = ApplicationId.New();
    }

    public static Application Create(
        string clientId,
        string displayName,
        string? clientSecret = null,
        IEnumerable<string>? redirectUris = null,
        IEnumerable<string>? postLogoutRedirectUris = null,
        string? permissions = null,
        string clientType = "confidential",
        string applicationType = "web",
        Guid? createdById = null)
    {
        return new Application
        {
            ClientId = clientId,
            DisplayName = displayName,
            ClientSecret = clientSecret,
            RedirectUris = RedirectUriCollection.Create(redirectUris),
            PostLogoutRedirectUris = RedirectUriCollection.Create(postLogoutRedirectUris),
            Permissions = permissions,
            ClientType = clientType,
            ApplicationType = applicationType,
            ConcurrencyToken = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById,
            IsActive = true
        };
    }

    public void Update(
        string? displayName,
        IEnumerable<string>? redirectUris,
        IEnumerable<string>? postLogoutRedirectUris,
        string? permissions,
        string? clientType,
        Guid? updatedById = null)
    {
        DisplayName = displayName;
        RedirectUris = RedirectUriCollection.Create(redirectUris);
        PostLogoutRedirectUris = RedirectUriCollection.Create(postLogoutRedirectUris);
        Permissions = permissions;
        ClientType = clientType;

        UpdatedAt = DateTime.UtcNow;
        UpdatedById = updatedById;
    }

    public void UpdateRedirectUris(IEnumerable<string> redirectUris, Guid? updatedById = null)
    {
        RedirectUris = RedirectUriCollection.Create(redirectUris);
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = updatedById;
    }

    public void RotateSecret(string newSecretHash, Guid? updatedById = null)
    {
        ClientSecret = newSecretHash;
        ConcurrencyToken = Guid.NewGuid().ToString();
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = updatedById;
    }

    public void SetStatus(bool isActive, Guid? updatedById = null)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
        UpdatedById = updatedById;
    }
}
