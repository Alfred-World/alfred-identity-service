using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.RemovePermissions;

public record RemovePermissionsFromRoleCommand(long RoleId, List<long> PermissionIds)
    : IRequest<RemovePermissionsFromRoleResult>;

public record RemovePermissionsFromRoleResult(bool Success, string? Error = null);
