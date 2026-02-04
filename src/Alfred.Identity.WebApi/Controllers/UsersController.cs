using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Users.Commands.AssignRoles;
using Alfred.Identity.Application.Users.Commands.Ban;
using Alfred.Identity.Application.Users.Commands.RevokeRoles;
using Alfred.Identity.Application.Users.Commands.Unban;
using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.Application.Users.Queries.GetActivityLogs;
using Alfred.Identity.Application.Users.Queries.GetBanHistory;
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
    [ProducesResponseType(typeof(ApiSuccessResponse<AssignRolesToUserResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssignRolesToUserResult>> AssignRoles(Guid userId, [FromBody] List<Guid> roleIds)
    {
        var result = await _mediator.Send(new AssignRolesToUserCommand(userId, roleIds));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return OkResponse(result);
    }

    /// <summary>
    /// Revoke roles from a user
    /// </summary>
    /// <param name="userId">ID of the user</param>
    /// <param name="roleIds">List of role IDs to revoke</param>
    [HttpDelete("{userId}/roles")]
    [ProducesResponseType(typeof(ApiSuccessResponse<RevokeRolesFromUserResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RevokeRolesFromUserResult>> RevokeRoles(Guid userId, [FromBody] List<Guid> roleIds)
    {
        var result = await _mediator.Send(new RevokeRolesFromUserCommand(userId, roleIds));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return OkResponse(result);
    }

    /// <summary>
    /// Ban a user
    /// </summary>
    [HttpPost("{userId}/ban")]
    [ProducesResponseType(typeof(ApiSuccessResponse<BanUserResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserRequest request)
    {
        var command = new BanUserCommand(userId, request.Reason, request.ExpiresAt);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return OkResponse(result);
    }

    /// <summary>
    /// Unban a user
    /// </summary>
    [HttpPost("{userId}/unban")]
    [ProducesResponseType(typeof(ApiSuccessResponse<UnbanUserResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbanUser(Guid userId)
    {
        var command = new UnbanUserCommand(userId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return OkResponse(result);
    }

    /// <summary>
    /// Get user ban history
    /// </summary>
    [HttpGet("{userId}/ban-history")]
    [ProducesResponseType(typeof(ApiSuccessResponse<List<BanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanHistory(Guid userId)
    {
        var query = new GetUserBanHistoryQuery(userId);
        var result = await _mediator.Send(query);
        return OkResponse(result);
    }

    /// <summary>
    /// Get user activity logs
    /// </summary>
    [HttpGet("{userId}/activities")]
    [ProducesResponseType(typeof(ApiPagedResponse<ActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivityLogs(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var query = new GetUserActivityLogsQuery(userId, page, pageSize);
        var result = await _mediator.Send(query);
        return OkPaginatedResponse(new PageResult<ActivityLogDto>(result.Items, result.Page, result.PageSize, result.TotalCount));
    }
}

public record BanUserRequest(string Reason, DateTime? ExpiresAt = null);

