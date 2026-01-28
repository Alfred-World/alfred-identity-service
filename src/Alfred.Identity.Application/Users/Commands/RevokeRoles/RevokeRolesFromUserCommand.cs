using MediatR;

namespace Alfred.Identity.Application.Users.Commands.RevokeRoles;

public record RevokeRolesFromUserCommand(long UserId, List<long> RoleIds) : IRequest<RevokeRolesFromUserResult>;

public record RevokeRolesFromUserResult(bool Success, string? Error = null);
