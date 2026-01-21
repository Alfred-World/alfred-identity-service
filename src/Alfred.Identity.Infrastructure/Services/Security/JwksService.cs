using System.Security.Cryptography;
using System.Text.Json;

using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Services;
using Alfred.Identity.Domain.Entities;

using Microsoft.IdentityModel.Tokens;
// Need System.IdentityModel.Tokens.Jwt or Microsoft.IdentityModel.Tokens

namespace Alfred.Identity.Infrastructure.Services.Security;

// using MediatR; removed

public class JwksService : IJwksService
{
    private readonly ISigningKeyRepository _keyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public JwksService(ISigningKeyRepository keyRepository, IUnitOfWork unitOfWork)
    {
        _keyRepository = keyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<object> GetJsonWebKeySetAsync(CancellationToken cancellationToken = default)
    {
        var keys = await _keyRepository.GetValidKeysAsync(cancellationToken);

        // Auto-generate if empty (Basic seeding for dev/first run)
        if (!keys.Any())
        {
            var newKey = GenerateNewRsaKey();
            await _keyRepository.AddAsync(newKey, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            keys = new[] { newKey };
        }

        // Build clean JWK response without unsupported parameters (like "oth")
        var jwkList = new List<object>();
        foreach (var key in keys)
        {
            try
            {
                // Parse the stored JWK JSON
                var storedJwk = JsonSerializer.Deserialize<Dictionary<string, object>>(key.PublicKey);
                if (storedJwk != null)
                {
                    // Create a clean JWK with only standard public key parameters
                    var cleanJwk = new Dictionary<string, object>
                    {
                        ["kty"] = storedJwk.TryGetValue("kty", out var kty) ? kty : "RSA",
                        ["kid"] = key.KeyId,
                        ["use"] = "sig",
                        ["alg"] = key.Algorithm,
                        ["n"] = storedJwk.TryGetValue("n", out var n) ? n : storedJwk.TryGetValue("N", out var N) ? N : "",
                        ["e"] = storedJwk.TryGetValue("e", out var e) ? e : storedJwk.TryGetValue("E", out var E) ? E : ""
                    };
                    jwkList.Add(cleanJwk);
                }
            }
            catch
            {
                // Fallback or log. For now ignore malformed.
            }
        }

        return new { keys = jwkList };
    }

    private SigningKey GenerateNewRsaKey()
    {
        using var rsa = RSA.Create(2048);
        var keyId = Guid.NewGuid().ToString("N");

        // Export parameters
        var privateKeyParams = rsa.ExportParameters(true);
        var publicKeyParams = rsa.ExportParameters(false);

        // We can convert to JWK format directly using Microsoft.IdentityModel.Tokens
        var rsaKey = new RsaSecurityKey(rsa) { KeyId = keyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(rsaKey);

        // Serialize
        var publicKeyJson = JsonSerializer.Serialize(jwk); // This serializes the JsonWebKey object to JSON

        // For Private Key, we need to be careful. JsonWebKey might not include private components if created from RsaSecurityKey?
        // Actually ConvertFromRSASecurityKey might not include private params unless configured.
        // It's safer to store parameters or PEM.
        // But for simplicity of reloading:

        // Let's store serialization of RSAParameters or standard JWK with private fields?
        // Standard JWK has 'd', 'p', 'q', etc.

        // Re-create RSA key to export full JWK
        // Internal helper to get full JSON?

        // Let's stick to: PublicKey = JWK JSON (safe to share)
        // PrivateKey = XML or JSON of RSAParameters (kept secret)

        // var privateKeyJson = rsa.ToXmlString(true); // Legacy but easy.
        // Or JSON serialize RSAParameters.

        // Doing JSON of RSAParameters for PrivateKey
        var privateKeyJson = JsonSerializer.Serialize(privateKeyParams);

        return SigningKey.Create(
            keyId,
            publicKeyJson, // JWK format
            privateKeyJson, // RSAParameters JSON
            "RS256",
            true
        );
    }
}
