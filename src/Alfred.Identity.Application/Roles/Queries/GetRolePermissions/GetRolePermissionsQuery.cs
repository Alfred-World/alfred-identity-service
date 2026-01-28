using Alfred.Identity.Application.Permissions.Common;

using MediatR;

namespace Alfred.Identity.Application.Roles.Queries.GetRolePermissions;

public record GetRolePermissionsQuery(long RoleId) : IRequest<List<PermissionDto>>;
