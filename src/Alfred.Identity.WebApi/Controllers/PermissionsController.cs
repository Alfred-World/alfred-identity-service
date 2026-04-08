using Alfred.Identity.Application.Permissions;
using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Domain.Querying;
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

    /// <summary>Search permissions with typed filter (POST)</summary>
    [HttpPost("search")]
    [RequirePermission("permissions:read")]
    [ProducesResponseType(typeof(ApiPagedResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPermissions(
        [FromBody] SearchRequest<PermissionFilterInput> request,
        CancellationToken cancellationToken)
    {
        var result = await _permissionService.SearchPermissionsAsync(request.ToSearchRequest(), cancellationToken);
        return OkPaginatedResponse(result);
    }

    /// <summary>Get search metadata for permissions</summary>
    [HttpGet("search/metadata")]
    [RequirePermission("permissions:read")]
    [ProducesResponseType(typeof(ApiResponse<SearchMetadataResponse>), StatusCodes.Status200OK)]
    public IActionResult GetSearchMetadata()
    {
        var metadata = _permissionService.GetSearchMetadata();
        return OkResponse(metadata);
    }
}
