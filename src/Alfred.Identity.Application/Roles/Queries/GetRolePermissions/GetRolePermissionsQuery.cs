using Alfred.Identity.Application.Permissions.Common;

using MediatR;

namespace Alfred.Identity.Application.Roles.Queries.GetRolePermissions;

public record GetRolePermissionsQuery(Guid RoleId) : IRequest<List<PermissionDto>>;
