using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Users;
using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.WebApi.Contracts.Common;
using Alfred.Identity.WebApi.Contracts.Users;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("identity/mgmt/users")]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Get paginated list of users</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var result = await _userService.GetAllUsersAsync(queryRequest.ToQueryRequest(), cancellationToken);
        return OkPaginatedResponse(result);
    }

    /// <summary>Get user by ID</summary>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync(userId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("User not found");
        }

        return OkResponse(result);
    }

    /// <summary>Assign roles to a user</summary>
    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoles(Guid userId, [FromBody] List<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        await _userService.AssignRolesAsync(userId, roleIds, cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Revoke roles from a user</summary>
    [HttpDelete("{userId:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeRoles(Guid userId, [FromBody] List<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        await _userService.RevokeRolesAsync(userId, roleIds, cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Ban a user</summary>
    [HttpPost("{userId:guid}/ban")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.BanUserAsync(userId, request.Reason, request.ExpiresAt, cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Unban a user</summary>
    [HttpPost("{userId:guid}/unban")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbanUser(Guid userId, CancellationToken cancellationToken)
    {
        await _userService.UnbanUserAsync(userId, cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Get user ban history</summary>
    [HttpGet("{userId:guid}/ban-history")]
    [ProducesResponseType(typeof(ApiResponse<List<BanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanHistory(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetBanHistoryAsync(userId, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>Get user activity logs</summary>
    [HttpGet("{userId:guid}/activities")]
    [ProducesResponseType(typeof(ApiPagedResponse<ActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivityLogs(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetActivityLogsAsync(userId, page, pageSize, cancellationToken);
        return OkPaginatedResponse(new PageResult<ActivityLogDto>(result.Items, result.Page, result.PageSize,
            result.TotalCount));
    }

    /// <summary>Admin force-reset a user's password (no old password required)</summary>
    [HttpPost("{userId:guid}/reset-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminResetPassword(
        Guid userId,
        [FromBody] AdminResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.AdminResetPasswordAsync(userId, request.NewPassword, cancellationToken);
        return OkResponse("Password has been reset successfully.");
    }
}
