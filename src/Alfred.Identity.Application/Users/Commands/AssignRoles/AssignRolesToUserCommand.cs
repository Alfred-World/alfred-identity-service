using MediatR;

namespace Alfred.Identity.Application.Users.Commands.AssignRoles;

public record AssignRolesToUserCommand(Guid UserId, List<Guid> RoleIds) : IRequest<AssignRolesToUserResult>;

public record AssignRolesToUserResult(bool Success, string? Error = null);
