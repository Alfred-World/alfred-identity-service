namespace Alfred.Identity.Domain.Abstractions.Services;

/// <summary>
/// Service for handling authorization codes and PKCE validation
/// </summary>
public interface IAuthorizationCodeService
{
    /// <summary>
    /// Generates a unified authorization code string
    /// </summary>
    string GenerateAuthorizationCode();

    /// <summary>
    /// Validates the PKCE code verifier against the challenge
    /// </summary>
    bool ValidatePkce(string codeChallenge, string codeChallengeMethod, string codeVerifier);
    
    /// <summary>
    /// Hashes the authorization code for storage (if we decide to hash it like refresh tokens)
    /// </summary>
    string HashAuthorizationCode(string code);
}
