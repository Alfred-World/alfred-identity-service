using System.Text.Json.Serialization;

namespace Alfred.Identity.Application.Auth.Common;

/// <summary>
/// OAuth2/OIDC token endpoint response — conforms to RFC 6749.
/// Field names are snake_case to match the OAuth2 spec.
/// </summary>
public sealed record TokenResponseDto
{
    [JsonPropertyName("access_token")] public string? AccessToken { get; init; }

    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; init; }

    [JsonPropertyName("id_token")] public string? IdToken { get; init; }

    [JsonPropertyName("token_type")] public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expires_in")] public int ExpiresIn { get; init; }
}
