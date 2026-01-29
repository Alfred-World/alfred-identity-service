using System.Text.Json;

namespace Alfred.Identity.Application.Applications.Common;

/// <summary>
/// DTO for Application entity - shared between queries and responses
/// </summary>
public sealed record ApplicationDto
{
    public long Id { get; init; }
    public string ClientId { get; init; } = null!;
    // Only populated when creating or generating a new secret
    public string? ClientSecret { get; init; }
    public string? DisplayName { get; init; }
    public List<string>? RedirectUris { get; init; }
    public List<string>? PostLogoutRedirectUris { get; init; }
    public List<string>? Permissions { get; init; }
    public string? ApplicationType { get; init; }
    public string? ClientType { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }

    public static ApplicationDto FromEntity(Domain.Entities.Application app)
    {
        return new ApplicationDto
        {
            Id = app.Id,
            ClientId = app.ClientId,
            DisplayName = app.DisplayName,
            RedirectUris = ParseJsonList(app.RedirectUris),
            PostLogoutRedirectUris = ParseJsonList(app.PostLogoutRedirectUris),
            Permissions = ParseJsonList(app.Permissions),
            ApplicationType = app.ApplicationType,
            ClientType = app.ClientType,
            IsActive = app.IsActive,
            CreatedAt = app.CreatedAt,
            UpdatedAt = app.UpdatedAt
        };
    }

    private static List<string>? ParseJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            // Fallback for legacy plain text or invalid JSON
            return new List<string>();
        }
    }
}
