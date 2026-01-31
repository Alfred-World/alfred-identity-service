using MediatR;

namespace Alfred.Identity.Application.Users.Commands.RevokeRoles;

public record RevokeRolesFromUserCommand(Guid UserId, List<Guid> RoleIds) : IRequest<RevokeRolesFromUserResult>;

public record RevokeRolesFromUserResult(bool Success, string? Error = null);
