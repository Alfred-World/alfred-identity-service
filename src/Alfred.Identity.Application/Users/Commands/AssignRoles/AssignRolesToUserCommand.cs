using MediatR;

namespace Alfred.Identity.Application.Users.Commands.AssignRoles;

public record AssignRolesToUserCommand(long UserId, List<long> RoleIds) : IRequest<AssignRolesToUserResult>;

public record AssignRolesToUserResult(bool Success, string? Error = null);
