using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.AddPermissions;

public record AddPermissionsToRoleCommand(long RoleId, List<long> PermissionIds) : IRequest<AddPermissionsToRoleResult>;

public record AddPermissionsToRoleResult(bool Success, string? Error = null);
