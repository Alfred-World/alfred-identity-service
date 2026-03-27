using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.Application.Users;
using Alfred.Identity.Application.Users.Common;
using Alfred.Identity.WebApi.Contracts.Common;
using Alfred.Identity.WebApi.Contracts.Users;
using Alfred.Identity.WebApi.Filters;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("identity/mgmt/users")]
[Authorize]
[RequireAuthenticatedUser]
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>Get paginated list of users</summary>
    [HttpGet]
    [RequirePermission("users:read")]
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
    [RequirePermission("users:read")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserById(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetUserByIdAsync((UserId)userId, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("User not found");
        }

        return OkResponse(result);
    }

    /// <summary>Create a new user</summary>
    [HttpPost]
    [RequirePermission("users:create")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var createdUser = await _userService.CreateUserAsync(
            new CreateUserInput(
                request.Email,
                request.Password,
                request.FullName,
                request.UserName,
                request.RoleIds),
            cancellationToken);

        return CreatedResponse(createdUser, "User created successfully.");
    }

    /// <summary>Assign roles to a user</summary>
    [HttpPost("{userId:guid}/roles")]
    [RequirePermission("users:update")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoles(Guid userId, [FromBody] List<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        await _userService.AssignRolesAsync((UserId)userId, roleIds.Select(x => (RoleId)x).ToList(), cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Revoke roles from a user</summary>
    [HttpDelete("{userId:guid}/roles")]
    [RequirePermission("users:update")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeRoles(Guid userId, [FromBody] List<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        await _userService.RevokeRolesAsync((UserId)userId, roleIds.Select(x => (RoleId)x).ToList(), cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Ban a user</summary>
    [HttpPost("{userId:guid}/ban")]
    [RequirePermission("users:ban")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BanUser(Guid userId, [FromBody] BanUserRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.BanUserAsync((UserId)userId, request.Reason, request.ExpiresAt, cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Unban a user</summary>
    [HttpPost("{userId:guid}/unban")]
    [RequirePermission("users:unban")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnbanUser(Guid userId, CancellationToken cancellationToken)
    {
        await _userService.UnbanUserAsync((UserId)userId, cancellationToken);
        return OkResponse(true);
    }

    /// <summary>Get user ban history</summary>
    [HttpGet("{userId:guid}/ban-history")]
    [RequirePermission("users:read")]
    [ProducesResponseType(typeof(ApiResponse<List<BanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBanHistory(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _userService.GetBanHistoryAsync((UserId)userId, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>Get user activity logs</summary>
    [HttpGet("{userId:guid}/activities")]
    [RequirePermission("users:read")]
    [ProducesResponseType(typeof(ApiPagedResponse<ActivityLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivityLogs(
        Guid userId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _userService.GetActivityLogsAsync((UserId)userId, page, pageSize, cancellationToken);
        return OkPaginatedResponse(new PageResult<ActivityLogDto>(result.Items, result.Page, result.PageSize,
            result.TotalCount));
    }

    /// <summary>Admin force-reset a user's password (no old password required)</summary>
    [HttpPost("{userId:guid}/reset-password")]
    [RequirePermission("users:update")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdminResetPassword(
        Guid userId,
        [FromBody] AdminResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.AdminResetPasswordAsync((UserId)userId, request.NewPassword, cancellationToken);
        return OkResponse("Password has been reset successfully.");
    }

    /// <summary>Admin confirms a user's email without requiring token confirmation flow.</summary>
    [HttpPost("{userId:guid}/confirm-email")]
    [RequirePermission("users:confirm-email")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdminConfirmEmail(Guid userId, CancellationToken cancellationToken)
    {
        await _userService.AdminConfirmEmailAsync((UserId)userId, cancellationToken);
        return OkResponse("Email has been confirmed successfully.");
    }
}
