namespace Alfred.Identity.Domain.Abstractions.Services;

/// <summary>
/// Provides JSON Web Key Set (JWKS) functionality for token signing/verification
/// </summary>
public interface IJwksService
{
    /// <summary>
    /// Get the JWKS containing all valid public keys for token verification.
    /// Returns object to avoid coupling Domain to Microsoft.IdentityModel.Tokens.
    /// Actual implementation returns JsonWebKeySet.
    /// </summary>
    Task<object> GetJsonWebKeySetAsync(CancellationToken cancellationToken = default);
}
