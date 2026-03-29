using System.Text.Json;

namespace Alfred.Identity.Application.Applications.Common;

/// <summary>
/// DTO for Application entity - shared between queries and responses
/// </summary>
public sealed class ApplicationDto
{
    public Guid Id { get; set; }

    public string? ClientId { get; set; }

    // Only populated when creating or generating a new secret
    public string? ClientSecret { get; set; }
    public string? DisplayName { get; set; }
    public List<string>? RedirectUris { get; set; }
    public List<string>? PostLogoutRedirectUris { get; set; }
    public List<string>? Permissions { get; set; }
    public string? ApplicationType { get; set; }
    public string? ClientType { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public static ApplicationDto FromEntity(Domain.Entities.Application app)
    {
        return new ApplicationDto
        {
            Id = app.Id.Value,
            ClientId = app.ClientId,
            DisplayName = app.DisplayName,
            RedirectUris = app.RedirectUris.Uris.Count > 0 ? [.. app.RedirectUris.Uris] : null,
            PostLogoutRedirectUris =
                app.PostLogoutRedirectUris.Uris.Count > 0 ? [.. app.PostLogoutRedirectUris.Uris] : null,
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
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json);
        }
        catch
        {
            return [];
        }
    }
}
