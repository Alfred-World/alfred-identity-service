using Alfred.Identity.Application.Permissions;
using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.WebApi.Filters;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("identity/permissions")]
[Authorize]
[RequireAuthenticatedUser]
public class PermissionsController : BaseApiController
{
    private readonly IPermissionService _permissionService;

    public PermissionsController(IPermissionService permissionService)
    {
        _permissionService = permissionService;
    }

    /// <summary>Get paginated list of permissions</summary>
    [HttpGet]
    [RequirePermission("permissions:read")]
    [ProducesResponseType(typeof(ApiPagedResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPermissions(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.GetAllPermissionsAsync(queryRequest.ToQueryRequest(), cancellationToken);
        return OkPaginatedResponse(result);
    }
}
