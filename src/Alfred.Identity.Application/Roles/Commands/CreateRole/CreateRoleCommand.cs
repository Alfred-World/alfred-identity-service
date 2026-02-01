using Alfred.Identity.Application.Roles.Common;

using MediatR;

namespace Alfred.Identity.Application.Roles.Commands.CreateRole;

public record CreateRoleCommand(
    string Name,
    string? Icon = null,
    bool IsImmutable = false,
    bool IsSystem = false,
    List<Guid>? Permissions = null) : IRequest<CreateRoleResult>;

public record CreateRoleResult(bool Success, Guid? RoleId = null, RoleDto? Data = null, string? Error = null);
