using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Roles.Common;

using MediatR;

namespace Alfred.Identity.Application.Roles.Queries.GetRoles;

/// <summary>
/// Query to get paginated list of roles
/// </summary>
public record GetRolesQuery(QueryRequest QueryRequest) : IRequest<PageResult<RoleDto>>;
