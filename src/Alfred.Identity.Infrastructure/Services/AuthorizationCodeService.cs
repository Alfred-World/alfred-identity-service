using System.Security.Cryptography;
using System.Text;
using Alfred.Identity.Domain.Abstractions.Services;

namespace Alfred.Identity.Infrastructure.Services;

public class AuthorizationCodeService : IAuthorizationCodeService
{
    public string GenerateAuthorizationCode()
    {
        // Generate a random 32-byte code and base64url encode it
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    public bool ValidatePkce(string codeChallenge, string codeChallengeMethod, string codeVerifier)
    {
        if (string.IsNullOrEmpty(codeChallenge) || string.IsNullOrEmpty(codeVerifier))
        {
            // If PKCE is not used/required, this might depend on policy. 
            // But if params are passed, they must be valid.
            // For this service, simply validation logic.
            return false; 
        }

        if (codeChallengeMethod == "plain")
        {
            return codeChallenge == codeVerifier;
        }

        if (codeChallengeMethod == "S256")
        {
            var codeVerifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(codeVerifierBytes);
            var computedChallenge = Base64UrlEncode(hashedBytes);
            return computedChallenge == codeChallenge;
        }

        // Unknown method
        return false;
    }

    public string HashAuthorizationCode(string code)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static string Base64UrlEncode(byte[] input)
    {
        var output = Convert.ToBase64String(input)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
        return output;
    }
}
