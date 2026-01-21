using Alfred.Identity.Application.Applications.Commands.Create;
using Alfred.Identity.Application.Applications.Commands.Update;

namespace Alfred.Identity.WebApi.Contracts.Applications;

/// <summary>
/// Extension methods for mapping application requests to commands
/// </summary>
public static class ApplicationRequestMappingExtensions
{
    /// <summary>
    /// Map CreateApplicationRequest to CreateApplicationCommand
    /// </summary>
    public static CreateApplicationCommand ToCreateCommand(this CreateApplicationRequest request)
    {
        return new CreateApplicationCommand(
            ClientId: request.ClientId,
            ClientSecret: request.ClientSecret,
            DisplayName: request.DisplayName,
            RedirectUris: request.RedirectUris,
            PostLogoutRedirectUris: request.PostLogoutRedirectUris ?? string.Empty,
            Permissions: request.Permissions ?? string.Empty,
            Type: request.Type
        );
    }

    /// <summary>
    /// Map UpdateApplicationRequest to UpdateApplicationCommand
    /// </summary>
    public static UpdateApplicationCommand ToUpdateCommand(this UpdateApplicationRequest request, long id)
    {
        return new UpdateApplicationCommand(
            Id: id,
            DisplayName: request.DisplayName,
            RedirectUris: request.RedirectUris,
            PostLogoutRedirectUris: request.PostLogoutRedirectUris,
            Permissions: request.Permissions
        );
    }
}
