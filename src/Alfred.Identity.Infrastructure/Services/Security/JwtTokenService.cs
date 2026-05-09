using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Alfred.Identity.Domain.Abstractions.Security;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Options;

using Microsoft.IdentityModel.Tokens;

using TokenValidationResult = Alfred.Identity.Domain.Abstractions.Security.TokenValidationResult;

namespace Alfred.Identity.Infrastructure.Services.Security;

/// <summary>
/// JWT token service implementation using RS256 (Asymmetric) backed by DB keys
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly ISigningKeyRepository _keyRepository;
    private readonly string _issuer;

    public int AccessTokenLifetimeSeconds { get; }
    public int RefreshTokenLifetimeSeconds { get; }

    public JwtTokenService(JwtSettings jwtSettings, ISigningKeyRepository keyRepository)
    {
        _keyRepository = keyRepository;
        _issuer = jwtSettings.Issuer;
        AccessTokenLifetimeSeconds = jwtSettings.AccessTokenLifetimeSeconds;
        RefreshTokenLifetimeSeconds = jwtSettings.RefreshTokenLifetimeSeconds;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAccessTokenAsync(
        Guid userId,
        string email,
        string? fullName,
        Guid applicationId,
        string clientId,
        Guid? authorizationId = null,
        string? scopes = null)
    {
        if (applicationId == Guid.Empty)
        {
            throw new ArgumentOutOfRangeException(nameof(applicationId), "Application ID must be provided.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        var activeKey = await _keyRepository.GetActiveKeyAsync();
        if (activeKey == null)
        {
            throw new InvalidOperationException("No active signing key found. Ensure keys are initialized.");
        }

        var signingCredentials = CreateSigningCredentials(activeKey);
        var jwtId = Guid.NewGuid().ToString("N");
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new(JwtRegisteredClaimNames.Azp, clientId),
            new("client_id", clientId),
            new("app_id", applicationId.ToString())
        };

        if (!string.IsNullOrEmpty(fullName))
        {
            claims.Add(new Claim("name", fullName));
        }

        if (authorizationId.HasValue)
        {
            claims.Add(new Claim("authorization_id", authorizationId.Value.ToString()));
        }

        if (!string.IsNullOrWhiteSpace(scopes))
        {
            claims.Add(new Claim("scope", scopes));
        }

        var token = new JwtSecurityToken(
            _issuer,
            clientId,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddSeconds(AccessTokenLifetimeSeconds),
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
                    var jwk = new JsonWebKey(k.PublicKey);
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
                ValidateAudience = false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = securityKeys,
                ClockSkew = TimeSpan.Zero
            };

            SecurityToken? validatedToken = null;
            var principal = await Task.Run(() =>
                handler.ValidateToken(token, validationParameters, out validatedToken));

            var jwtToken = validatedToken as JwtSecurityToken;
            var userId = principal.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == JwtRegisteredClaimNames.Sub)
                ?.Value;
            var email = principal.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.Email || c.Type == JwtRegisteredClaimNames.Email)?.Value;
            var jwtId = principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            var clientId = principal.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value
                           ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Azp)?.Value;
            var applicationId = principal.Claims.FirstOrDefault(c => c.Type == "app_id")?.Value;
            var audience = jwtToken?.Audiences.FirstOrDefault()
                           ?? principal.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Aud)?.Value;

            return new TokenValidationResult
            {
                IsValid = true,
                UserId = Guid.TryParse(userId, out var id) ? id : null,
                Email = email,
                JwtId = jwtId,
                ClientId = clientId,
                ApplicationId = Guid.TryParse(applicationId, out var appId) ? appId : null,
                Audience = audience
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
    public async Task<string> GenerateIdTokenAsync(Guid userId, string email, string? fullName, string clientId,
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
            new(JwtRegisteredClaimNames.Azp, clientId)
        };

        if (!string.IsNullOrEmpty(fullName))
        {
            claims.Add(new Claim("name", fullName));
        }

        if (!string.IsNullOrEmpty(nonce))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, nonce));
        }

        var token = new JwtSecurityToken(
            _issuer,
            clientId,
            claims,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            signingCredentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private SigningCredentials CreateSigningCredentials(SigningKey key)
    {
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

    private static byte[] Base64UrlDecode(string arg)
    {
        var s = arg.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 0: break;
            case 2: s += "=="; break;
            case 3: s += "="; break;
            default: throw new FormatException("Illegal base64url string!");
        }
        return Convert.FromBase64String(s);
    }
}
