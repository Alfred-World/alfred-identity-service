namespace Alfred.Identity.Application.Applications.Common;

public sealed record UpdateApplicationDto
{
    public Optional<string> DisplayName { get; init; }
    public Optional<string> RedirectUris { get; init; }
    public Optional<string?> PostLogoutRedirectUris { get; init; }
    public Optional<string?> Permissions { get; init; }
}
