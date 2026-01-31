using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.RemovePermissions;

public record RemovePermissionsFromRoleCommand(Guid RoleId, List<Guid> PermissionIds)
    : IRequest<RemovePermissionsFromRoleResult>;

public record RemovePermissionsFromRoleResult(bool Success, string? Error = null);
