using Alfred.Identity.Application.Common;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.Domain.Entities;

using MediatR;


namespace Alfred.Identity.Application.Auth.Commands.ExternalLogin;

public record LoginWithExternalProviderCommand(
    string Provider,
    string ProviderKey,
    string? Email,
    string? DisplayName
) : IRequest<Result<LoginWithExternalProviderResult>>;

public record LoginWithExternalProviderResult(UserDto User, bool IsNewUser);
