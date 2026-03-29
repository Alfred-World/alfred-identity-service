using Alfred.Identity.Domain.Common.Enums;

namespace Alfred.Identity.Domain.Entities;

/// <summary>
/// Represents a token (RefreshToken, AccessToken reference, AuthorizationCode), aligned with OpenIddictTokens schema
/// </summary>
public sealed class Token : BaseEntity<TokenId>
{
    public ApplicationId? ApplicationId { get; private set; }
    public AuthorizationId? AuthorizationId { get; private set; }

    // User/Subject
    public string? Subject { get; private set; }
    public UserId? UserId { get; private set; }

    public string Type { get; private set; } = null!; // access_token, refresh_token, authorization_code
    public string? ReferenceId { get; private set; } // The actual token string/hash/id used for lookup
    public TokenStatus Status { get; private set; } = TokenStatus.Valid;
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
    public Application? Application { get; private set; }
    public Authorization? Authorization { get; private set; }
    public User? User { get; private set; }

    private Token()
    {
        Id = TokenId.New();
    }

    public static Token Create(
        string type,
        ApplicationId? applicationId,
        string subject,
        UserId? userId,
        DateTime? expirationDate,
        string? referenceId = null,
        AuthorizationId? authorizationId = null,
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
            Status = TokenStatus.Valid
        };
    }

    public void Revoke()
    {
        Status = TokenStatus.Revoked;
    }

    public void Redeem()
    {
        RedemptionDate = DateTime.UtcNow;
        Status = TokenStatus.Redeemed;
    }
}
