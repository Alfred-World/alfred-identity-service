namespace Alfred.Identity.Domain.Entities;

using Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Represents an authorization, aligned with OpenIddictAuthorizations schema
/// </summary>
public class Authorization : BaseEntity
{
    public long ApplicationId { get; private set; }
    public long UserId { get; private set; } // Subject
    public string? Subject { get; private set; } // String representation of UserId if needed, or strictly UserId
    
    public string Status { get; private set; } = "Valid"; // Valid/Revoked/Inactive
    public string Type { get; private set; } = "Permanent"; // Permanent/AdHoc
    public string? Scopes { get; private set; } // Space delimited
    public string? Properties { get; private set; } // JSON
    public string? ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString();
    public DateTime CreationDate { get; private set; }

    // Navigation props
    public virtual Application Application { get; private set; } = null!;
    public virtual User User { get; private set; } = null!;

    private Authorization() { }

    public static Authorization Create(long applicationId, long userId, string scopes, string type = "Permanent")
    {
        return new Authorization
        {
            ApplicationId = applicationId,
            UserId = userId,
            Subject = userId.ToString(),
            Scopes = scopes,
            Type = type,
            Status = "Valid",
            ConcurrencyToken = Guid.NewGuid().ToString(),
            CreationDate = DateTime.UtcNow
        };
    }

    public void Revoke() { Status = "Revoked"; }
}
