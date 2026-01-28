using Alfred.Identity.Domain.Abstractions.Services;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[ApiController]
[Route(".well-known")]
public class WellKnownController : ControllerBase
{
    private readonly IJwksService _jwksService;
    private readonly IConfiguration _configuration;

    public WellKnownController(IJwksService jwksService, IConfiguration configuration)
    {
        _jwksService = jwksService;
        _configuration = configuration;
    }

    /// <summary>
    /// Get JSON Web Key Set (JWKS)
    /// </summary>
    /// <remarks>
    /// Returns the public keys used to verify JWT tokens signed by this identity provider.
    /// </remarks>
    [HttpGet("jwks.json")]
    public async Task<IActionResult> Jwks()
    {
        var jwks = await _jwksService.GetJsonWebKeySetAsync();
        return Ok(jwks);
    }

    /// <summary>
    /// Get OpenID Connect Configuration
    /// </summary>
    /// <remarks>
    /// Returns the OIDC discovery document containing standard endpoints and supported capabilities.
    /// </remarks>
    [HttpGet("openid-configuration")]
    public IActionResult OpenIdConfiguration()
    {
        var issuer = _configuration["Jwt:Issuer"] ?? $"{Request.Scheme}://{Request.Host}";

        return Ok(new
        {
            issuer = issuer,
            authorization_endpoint = $"{issuer}/connect/authorize",
            token_endpoint = $"{issuer}/connect/token",
            end_session_endpoint = $"{issuer}/connect/logout",
            jwks_uri = $"{issuer}/.well-known/jwks.json",
            userinfo_endpoint = $"{issuer}/connect/userinfo",
            response_types_supported = new[] { "code", "token", "id_token" },
            subject_types_supported = new[] { "public" },
            id_token_signing_alg_values_supported = new[] { "RS256" },
            scopes_supported = new[] { "openid", "profile", "email", "offline_access" },
            token_endpoint_auth_methods_supported = new[] { "client_secret_post", "client_secret_basic" },
            claims_supported = new[] { "sub", "iss", "aud", "exp", "iat", "email", "name", "preferred_username" }
        });
    }
}
