namespace Alfred.Identity.Application.Applications.Common;

/// <summary>
/// DTO for Application entity - shared between queries and responses
/// </summary>
public sealed record ApplicationDto
{
    public long Id { get; init; }
    public string ClientId { get; init; } = null!;
    public string? DisplayName { get; init; }
    public string? RedirectUris { get; init; }
    public string? PostLogoutRedirectUris { get; init; }
    public string? Permissions { get; init; }
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
            RedirectUris = app.RedirectUris,
            PostLogoutRedirectUris = app.PostLogoutRedirectUris,
            Permissions = app.Permissions,
            ApplicationType = app.ApplicationType,
            ClientType = app.ClientType,
            IsActive = app.IsActive,
            CreatedAt = app.CreatedAt,
            UpdatedAt = app.UpdatedAt
        };
    }
}
