using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Querying.Core;

using MediatR;

namespace Alfred.Identity.Application.Permissions.Queries.GetPermissions;

/// <summary>
/// Query to get paginated list of permissions
/// </summary>
public record GetPermissionsQuery(QueryRequest QueryRequest) : IRequest<PageResult<PermissionDto>>;
