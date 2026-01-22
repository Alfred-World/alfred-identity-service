using Alfred.Identity.Domain.Common;
using Alfred.Identity.Domain.Common.Base;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a token (RefreshToken, AccessToken reference, AuthorizationCode), aligned with OpenIddictTokens schema
/// </summary>
public class Token : BaseEntity
{
    public long? ApplicationId { get; private set; }
    public long? AuthorizationId { get; private set; }

    // User/Subject
    public string? Subject { get; private set; }
    public long? UserId { get; private set; }

    public string Type { get; private set; } = null!; // access_token, refresh_token, authorization_code
    public string? ReferenceId { get; private set; } // The actual token string/hash/id used for lookup
    public string? Status { get; private set; } = OAuthConstants.TokenStatus.Valid;
    public string? Payload { get; private set; } // Protected payload (JSON)
    public string? Properties { get; private set; } // JSON properties (e.g. device info)

    // Tracking info for management
    public string? IpAddress { get; private set; }
    public string? Location { get; private set; }
    public string? Device { get; private set; }

    public DateTime CreationDate { get; private set; }
    public DateTime? ExpirationDate { get; private set; }
    public DateTime? RedemptionDate { get; private set; } // For one-time use tokens like auth codes
    public string? ConcurrencyToken { get; private set; } = Guid.NewGuid().ToString();

    // Navigation properties
    public virtual Application? Application { get; private set; }
    public virtual Authorization? Authorization { get; private set; }
    public virtual User? User { get; private set; }

    private Token()
    {
    }

    public static Token Create(
        string type,
        long? applicationId,
        string subject,
        long? userId,
        DateTime? expirationDate,
        string? referenceId = null,
        long? authorizationId = null,
        string? payload = null,
        string? properties = null,
        string? ipAddress = null,
        string? location = null,
        string? device = null)
    {
        return new Token
        {
            Type = type,
            ApplicationId = applicationId,
            Subject = subject,
            UserId = userId,
            ExpirationDate = expirationDate,
            ReferenceId = referenceId,
            AuthorizationId = authorizationId,
            Payload = payload,
            Properties = properties,
            IpAddress = ipAddress,
            Location = location,
            Device = device,
            CreationDate = DateTime.UtcNow,
            ConcurrencyToken = Guid.NewGuid().ToString(),
            Status = OAuthConstants.TokenStatus.Valid
        };
    }

    public void Revoke()
    {
        Status = OAuthConstants.TokenStatus.Revoked;
    }

    public void Redeem()
    {
        RedemptionDate = DateTime.UtcNow;
        Status = OAuthConstants.TokenStatus.Redeemed;
    }
}

