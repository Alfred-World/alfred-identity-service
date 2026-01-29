using Alfred.Identity.Application.Users.Commands.AssignRoles;
using Alfred.Identity.Application.Users.Commands.RevokeRoles;
using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.Application.Users.Queries.GetUsers;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("users")]
public class UsersController : BaseApiController
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of users
    /// </summary>
    /// <remarks>
    /// Supports filtering, sorting, and pagination via query parameters.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var query = new GetUsersQuery(queryRequest.ToQueryRequest());
        var result = await _mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result);
    }

    /// <summary>
    /// Assign roles to a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="roleIds">List of role IDs to assign</param>
    [HttpPost("{userId}/roles")]
    [ProducesResponseType(typeof(AssignRolesToUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignRolesToUserResult>> AssignRoles(long userId, [FromBody] List<long> roleIds)
    {
        var result = await _mediator.Send(new AssignRolesToUserCommand(userId, roleIds));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Revoke roles from a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="roleIds">List of role IDs to revoke</param>
    [HttpDelete("{userId}/roles")]
    [ProducesResponseType(typeof(RevokeRolesFromUserResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RevokeRolesFromUserResult>> RevokeRoles(long userId, [FromBody] List<long> roleIds)
    {
        var result = await _mediator.Send(new RevokeRolesFromUserCommand(userId, roleIds));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
