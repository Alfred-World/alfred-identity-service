namespace Alfred.Identity.WebApi.Contracts.Connect;

// Connect Controller - OAuth2 Requests (RFC 6749 compliant naming)

/// <summary>
/// OAuth2 Authorization Request (RFC 6749)
/// </summary>
/// <param name="client_id">Client Identifier</param>
/// <param name="redirect_uri">URI to return the code/token to</param>
/// <param name="response_type">Response Type (e.g., 'code')</param>
/// <param name="scope">Requested scopes (space-separated)</param>
/// <param name="state">Client state for CSRF protection</param>
/// <param name="code_challenge">PKCE Code Challenge</param>
/// <param name="code_challenge_method">PKCE Method (S256)</param>
/// <param name="prompt">Prompt behavior (e.g., 'none', 'login')</param>
public record AuthorizeRequest(
    string client_id,
    string redirect_uri,
    string response_type,
    string scope,
    string? state,
    string? code_challenge,
    string? code_challenge_method,
    string? prompt
);

/// <summary>
/// OAuth2 Token Exchange Request (RFC 6749)
/// </summary>
/// <param name="grant_type">Grant Type ('authorization_code' or 'refresh_token')</param>
/// <param name="client_id">Client Identifier</param>
/// <param name="client_secret">Client Secret (for confidential clients)</param>
/// <param name="code">Authorization Code (for authorization_code grant)</param>
/// <param name="redirect_uri">Redirect URI used in authorize request</param>
/// <param name="code_verifier">PKCE Code Verifier</param>
/// <param name="refresh_token">Refresh Token (for refresh_token grant)</param>
/// <param name="scope">Requested scopes (optional)</param>
public record ExchangeCodeRequest(
    string grant_type,
    string? client_id,
    string? client_secret,
    string? code,
    string? redirect_uri,
    string? code_verifier,
    string? refresh_token,
    string? scope
);
