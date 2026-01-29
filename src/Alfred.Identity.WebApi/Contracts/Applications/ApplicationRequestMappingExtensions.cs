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
            request.ClientId,
            request.DisplayName,
            request.RedirectUris,
            request.PostLogoutRedirectUris ?? string.Empty,
            request.Permissions ?? string.Empty,
            request.Type
        );
    }

    /// <summary>
    /// Map UpdateApplicationRequest to UpdateApplicationCommand
    /// </summary>
    public static UpdateApplicationCommand ToUpdateCommand(this UpdateApplicationRequest request, long id)
    {
        return new UpdateApplicationCommand(
            id,
            request.DisplayName,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.Permissions
        );
    }
}
