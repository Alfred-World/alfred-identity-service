using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.AddPermissions;

public record AddPermissionsToRoleCommand(Guid RoleId, List<Guid> PermissionIds) : IRequest<AddPermissionsToRoleResult>;

public record AddPermissionsToRoleResult(bool Success, string? Error = null);
