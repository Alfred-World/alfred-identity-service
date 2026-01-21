namespace Alfred.Identity.WebApi.Contracts.Connect;

// Connect Controller - OAuth2 Requests (RFC 6749 compliant naming)

/// <summary>
/// OAuth2 Authorization Request (RFC 6749)
/// Uses snake_case to match OAuth2 spec
/// </summary>
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
/// Uses snake_case to match OAuth2 spec
/// </summary>
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
