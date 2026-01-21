using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;

using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using TokenValidationResult = Alfred.Identity.Domain.Abstractions.Security.TokenValidationResult;
// For simple deserialization if needed, though we use Jwks logic manually?

namespace Alfred.Identity.Infrastructure.Services.Security;

/// <summary>
/// JWT token service implementation using RS256 (Asymmetric) backed by DB keys
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;
    private readonly ISigningKeyRepository _keyRepository;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenLifetimeMinutes;

    public JwtTokenService(IConfiguration configuration, ISigningKeyRepository keyRepository)
    {
        _configuration = configuration;
        _keyRepository = keyRepository;
        _issuer = configuration["Jwt:Issuer"] ?? "alfred-identity";
        _audience = configuration["Jwt:Audience"] ?? "alfred-ecosystem";
        _accessTokenLifetimeMinutes = int.Parse(configuration["Jwt:AccessTokenLifetimeMinutes"] ?? "15");
    }

    /// <inheritdoc />
    public async Task<string> GenerateAccessTokenAsync(long userId, string email, string? fullName,
        long? applicationId = null)
    {
        var activeKey = await _keyRepository.GetActiveKeyAsync();
        if (activeKey == null)
        {
            // Should prompt initialization or throw. 
            // The JwksService auto-generates on call. Maybe calling JwksService here?
            // Or just throw for now, expecting JwksService initialization to have happened or seeding.
            throw new InvalidOperationException("No active signing key found. Ensure keys are initialized.");
        }

        var signingCredentials = CreateSigningCredentials(activeKey);

        var jwtId = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrEmpty(fullName))
        {
            claims.Add(new Claim("name", fullName));
        }

        if (applicationId.HasValue)
        {
            claims.Add(new Claim("client_id", applicationId.Value.ToString()));
        }

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes),
            signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc />
    public string? GetJwtIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        }
        catch
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TokenValidationResult> ValidateTokenAsync(string token)
    {
        try
        {
            var keys = await _keyRepository.GetValidKeysAsync();
            var securityKeys = new List<SecurityKey>();

            foreach (var k in keys)
            {
                try
                {
                    // Reconstruct RSA Public Key for validation
                    var jwk = new JsonWebKey(k.PublicKey); // Assuming PublicKey is JWK JSON
                    securityKeys.Add(jwk);
                }
                catch
                {
                }
            }

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = securityKeys, // Support multiple keys for rotation
                ClockSkew = TimeSpan.Zero
            };

            var principal = await Task.Run(() =>
                handler.ValidateToken(token, validationParameters, out var validatedToken));

            var userId = principal.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub)
                ?.Value;
            var email = principal.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == JwtRegisteredClaimNames.Email)?.Value;
            var jwtId = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

            return new TokenValidationResult
            {
                IsValid = true,
                UserId = long.TryParse(userId, out var id) ? id : null,
                Email = email,
                JwtId = jwtId
            };
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenValidationResult { IsValid = false, Error = "Token has expired" };
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return new TokenValidationResult { IsValid = false, Error = "Invalid token signature" };
        }
        catch (Exception ex)
        {
            return new TokenValidationResult { IsValid = false, Error = ex.Message };
        }
    }

    /// <inheritdoc />
    public string HashRefreshToken(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    /// <inheritdoc />
    public async Task<string> GenerateIdTokenAsync(long userId, string email, string? fullName, string clientId,
        string? nonce = null)
    {
        var activeKey = await _keyRepository.GetActiveKeyAsync();
        if (activeKey == null)
        {
            throw new InvalidOperationException("No active signing key found. Ensure keys are initialized.");
        }

        var signingCredentials = CreateSigningCredentials(activeKey);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.AuthTime, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Azp, clientId) // Authorized party
        };

        if (!string.IsNullOrEmpty(fullName))
        {
            claims.Add(new Claim("name", fullName));
        }

        if (!string.IsNullOrEmpty(nonce))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, nonce));
        }

        // ID Token typically has shorter lifetime (1 hour)
        var token = new JwtSecurityToken(
            _issuer,
            clientId, // For ID token, audience is the client_id
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SigningCredentials CreateSigningCredentials(SigningKey key)
    {
        // Private Key is stored as JWK-like format with Base64URL encoded values
        var rsa = RSA.Create();

        using var doc = JsonDocument.Parse(key.PrivateKey);
        var root = doc.RootElement;

        var rsaParams = new RSAParameters
        {
            Modulus = Base64UrlDecode(root.GetProperty("n").GetString()!),
            Exponent = Base64UrlDecode(root.GetProperty("e").GetString()!),
            D = Base64UrlDecode(root.GetProperty("d").GetString()!),
            P = Base64UrlDecode(root.GetProperty("p").GetString()!),
            Q = Base64UrlDecode(root.GetProperty("q").GetString()!),
            DP = Base64UrlDecode(root.GetProperty("dp").GetString()!),
            DQ = Base64UrlDecode(root.GetProperty("dq").GetString()!),
            InverseQ = Base64UrlDecode(root.GetProperty("qi").GetString()!)
        };

        rsa.ImportParameters(rsaParams);

        var securityKey = new RsaSecurityKey(rsa) { KeyId = key.KeyId };
        return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url
            .Replace('-', '+')
            .Replace('_', '/');

        // Add padding if necessary
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        return Convert.FromBase64String(base64);
    }
}
