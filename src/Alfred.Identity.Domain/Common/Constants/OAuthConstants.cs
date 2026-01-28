namespace Alfred.Identity.Domain.Common.Constants;

/// <summary>
/// Constants for OAuth2/OpenID Connect protocol values
/// </summary>
public static class OAuthConstants
{
    /// <summary>
    /// OAuth2 Grant Types
    /// </summary>
    public static class GrantTypes
    {
        public const string AuthorizationCode = "authorization_code";
        public const string RefreshToken = "refresh_token";
        public const string ClientCredentials = "client_credentials";
        public const string Password = "password";
    }

    /// <summary>
    /// Token Types
    /// </summary>
    public static class TokenTypes
    {
        public const string AccessToken = "access_token";
        public const string RefreshToken = "refresh_token";
        public const string AuthorizationCode = "authorization_code";
        public const string IdToken = "id_token";
    }

    /// <summary>
    /// Token Status Values
    /// </summary>
    public static class TokenStatus
    {
        public const string Valid = "Valid";
        public const string Revoked = "Revoked";
        public const string Redeemed = "Redeemed";
        public const string Inactive = "Inactive";
        public const string Expired = "Expired";
    }

    /// <summary>
    /// OAuth2 Error Codes
    /// </summary>
    public static class Errors
    {
        public const string InvalidRequest = "invalid_request";
        public const string InvalidClient = "invalid_client";
        public const string InvalidGrant = "invalid_grant";
        public const string UnauthorizedClient = "unauthorized_client";
        public const string UnsupportedGrantType = "unsupported_grant_type";
        public const string InvalidScope = "invalid_scope";
        public const string ServerError = "server_error";
    }
}
