namespace Alfred.Identity.Domain.Abstractions.Security;

/// <summary>
/// Hashes and verifies OAuth2 client secrets.
/// <para>
/// Unlike user passwords, client secrets are long random values (≥ 32 bytes) so a
/// slow adaptive hash (BCrypt) adds unnecessary latency without extra security.
/// Instead we use <b>HMAC-SHA256</b> keyed with a server-side secret, making the stored
/// hash useless to an attacker who only has the database (no rainbow tables, no brute-force
/// without the server key).
/// </para>
/// </summary>
public interface IClientSecretHasher
{
    /// <summary>Returns the HMAC-SHA256 hex digest of <paramref name="rawSecret"/>.</summary>
    string HashSecret(string rawSecret);

    /// <summary>
    /// Compares <paramref name="rawSecret"/> against <paramref name="storedHash"/> using
    /// a constant-time comparison to prevent timing attacks.
    /// </summary>
    bool VerifySecret(string rawSecret, string storedHash);
}
