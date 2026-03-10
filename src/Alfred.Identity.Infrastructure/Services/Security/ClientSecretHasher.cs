using System.Security.Cryptography;
using System.Text;

using Alfred.Identity.Domain.Abstractions.Security;

namespace Alfred.Identity.Infrastructure.Services.Security;

/// <summary>
/// HMAC-SHA256 implementation of <see cref="IClientSecretHasher"/>.
/// <para>
/// The HMAC key is read from the <c>APP_SECRET_KEY</c> environment variable
/// (falls back to <c>NEXTAUTH_SECRET</c> for environments that already have it set).
/// The key must be at least 32 characters long.
/// </para>
/// <para>
/// Stored format: the raw lowercase hex string of the 32-byte HMAC digest (64 chars).
/// </para>
/// </summary>
public sealed class ClientSecretHasher : IClientSecretHasher
{
    private readonly byte[] _keyBytes;

    public ClientSecretHasher()
    {
        var key = Environment.GetEnvironmentVariable("APP_SECRET_KEY");

        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
        {
            throw new InvalidOperationException(
                "APP_SECRET_KEY (or NEXTAUTH_SECRET) must be set and at least 32 characters long.");
        }

        _keyBytes = Encoding.UTF8.GetBytes(key);
    }

    /// <inheritdoc />
    public string HashSecret(string rawSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawSecret);

        var data = Encoding.UTF8.GetBytes(rawSecret);
        var hash = HMACSHA256.HashData(_keyBytes, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <inheritdoc />
    public bool VerifySecret(string rawSecret, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(rawSecret) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        var expected = HashSecret(rawSecret);

        // CryptographicOperations.FixedTimeEquals prevents timing side-channel attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
