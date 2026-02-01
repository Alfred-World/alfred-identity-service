using Alfred.Identity.Application.Roles.Common;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.UpdateRole;

public record UpdateRoleCommand(
    Guid Id,
    string Name,
    string? Icon = null,
    bool IsImmutable = false,
    bool IsSystem = false,
    List<Guid>? Permissions = null) : IRequest<UpdateRoleResult>;

public record UpdateRoleResult(bool Success, RoleDto? Data = null, string? Error = null);
