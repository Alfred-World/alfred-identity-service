using Alfred.Identity.Application.Roles.Common;

using MediatR;

namespace Alfred.Identity.Application.Roles.Queries.GetRoleById;

public record GetRoleByIdQuery(long Id) : IRequest<RoleDto?>;
