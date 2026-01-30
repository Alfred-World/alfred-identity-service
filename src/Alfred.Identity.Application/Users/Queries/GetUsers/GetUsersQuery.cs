using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Users.Common;

using MediatR;

namespace Alfred.Identity.Application.Users.Queries.GetUsers;

/// <summary>
/// Query to get paginated list of users
/// </summary>
public record GetUsersQuery(QueryRequest QueryRequest) : IRequest<PageResult<UserDto>>;
