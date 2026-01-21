namespace Alfred.Identity.Domain.Entities;

using Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Represents a cryptographic signing key used for JWT signing and verification.
/// </summary>
public class SigningKey : BaseEntity
{
    /// <summary>
    /// The Key ID (kid) included in the JWT header.
    /// </summary>
    public string KeyId { get; private set; } = null!;

    /// <summary>
    /// The algorithm used (e.g., "RS256").
    /// </summary>
    public string Algorithm { get; private set; } = "RS256";

    /// <summary>
    /// The key type (e.g., "RSA").
    /// </summary>
    public string Type { get; private set; } = "RSA";

    /// <summary>
    /// The public key parameters in JSON format (standard JWK format).
    /// </summary>
    public string PublicKey { get; private set; } = null!;

    /// <summary>
    /// The private key parameters in JSON format.
    /// WARNING: In a real production environment, this should be encrypted at rest.
    /// </summary>
    public string PrivateKey { get; private set; } = null!;

    /// <summary>
    /// Whether this key is currently the primary signing key.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// When the key was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the key should be rotated/expired (optional).
    /// </summary>
    public DateTime? ExpiresAt { get; private set; }

    private SigningKey() { }

    public static SigningKey Create(
        string keyId, 
        string publicKey, 
        string privateKey, 
        string algorithm = "RS256", 
        bool isActive = true)
    {
        return new SigningKey
        {
            KeyId = keyId,
            PublicKey = publicKey,
            PrivateKey = privateKey,
            Algorithm = algorithm,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            // Example: Expire in 90 days
            ExpiresAt = DateTime.UtcNow.AddDays(90) 
        };
    }
}
