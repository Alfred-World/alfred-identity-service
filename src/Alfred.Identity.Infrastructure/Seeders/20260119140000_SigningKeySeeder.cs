using System.Security.Cryptography;
using System.Text.Json;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Seeding;
using Alfred.Identity.Infrastructure.Providers.PostgreSQL;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Alfred.Identity.Infrastructure.Seeders;

/// <summary>
/// Seeds initial RSA signing key for JWT token signing
/// This must run before any other operations that require JWT tokens
/// </summary>
public class SigningKeySeeder : BaseDataSeeder
{
    private readonly PostgreSqlDbContext _dbContext;

    public SigningKeySeeder(
        PostgreSqlDbContext dbContext,
        ILogger<SigningKeySeeder> logger)
        : base(logger)
    {
        _dbContext = dbContext;
    }

    // Using early timestamp to ensure it runs first
    public override string Name => "20260119140000_SigningKeySeeder";

    public override async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        LogInfo("Starting to seed signing key...");

        // Check if active key already exists
        if (await _dbContext.SigningKeys.AnyAsync(k => k.IsActive, cancellationToken))
        {
            LogInfo("Active signing key already exists, skipping seed");
            return;
        }

        // Generate RSA key pair
        using var rsa = RSA.Create(2048);
        var rsaParams = rsa.ExportParameters(true);

        var keyId = $"key-{Guid.NewGuid():N}";

        // Create JWK format for public key
        var publicKeyJwk = new
        {
            kty = "RSA",
            kid = keyId,
            use = "sig",
            alg = "RS256",
            n = Base64UrlEncode(rsaParams.Modulus!),
            e = Base64UrlEncode(rsaParams.Exponent!)
        };

        // Store private key parameters
        var privateKeyData = new
        {
            d = Base64UrlEncode(rsaParams.D!),
            p = Base64UrlEncode(rsaParams.P!),
            q = Base64UrlEncode(rsaParams.Q!),
            dp = Base64UrlEncode(rsaParams.DP!),
            dq = Base64UrlEncode(rsaParams.DQ!),
            qi = Base64UrlEncode(rsaParams.InverseQ!),
            n = Base64UrlEncode(rsaParams.Modulus!),
            e = Base64UrlEncode(rsaParams.Exponent!)
        };

        var signingKey = SigningKey.Create(
            keyId: keyId,
            publicKey: JsonSerializer.Serialize(publicKeyJwk),
            privateKey: JsonSerializer.Serialize(privateKeyData),
            algorithm: "RS256",
            isActive: true
        );

        await _dbContext.SigningKeys.AddAsync(signingKey, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        LogInfo($"Seeded signing key: {signingKey.KeyId}");
        LogSuccess();
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
