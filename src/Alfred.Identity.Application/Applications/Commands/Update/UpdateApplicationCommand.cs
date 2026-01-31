using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Common;

using MediatR;

namespace Alfred.Identity.Application.Applications.Commands.Update;

/// <summary>
/// Command to update an application
/// </summary>
public record UpdateApplicationCommand(
    Guid Id,
    string DisplayName,
    string RedirectUris,
    string? PostLogoutRedirectUris,
    string? Permissions
) : IRequest<Result<ApplicationDto>>;
