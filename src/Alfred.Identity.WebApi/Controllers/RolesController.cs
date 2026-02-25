using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Roles;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.WebApi.Contracts.Common;
using Alfred.Identity.WebApi.Contracts.Roles;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("identity/roles")]
public class RolesController : BaseApiController
{
    private readonly IRoleService _roleService;

    public RolesController(IRoleService roleService)
    {
        _roleService = roleService;
    }

    /// <summary>Get paginated list of roles</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var result = await _roleService.GetAllRolesAsync(queryRequest.ToQueryRequest(), cancellationToken);
        return OkPaginatedResponse(result);
    }

    /// <summary>Get role by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _roleService.GetRoleByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Role not found");
        }

        return OkResponse(result);
    }

    /// <summary>Get permissions of a role</summary>
    [HttpGet("{id:guid}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<PermissionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRolePermissions(Guid id, CancellationToken cancellationToken)
    {
        var result = await _roleService.GetRolePermissionsAsync(id, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>Create a new role</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _roleService.CreateRoleAsync(
            request.Name, request.Icon, request.IsImmutable, request.IsSystem, request.Permissions, cancellationToken);
        return CreatedResponse(result);
    }

    /// <summary>Update an existing role</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken)
    {
        var result = await _roleService.UpdateRoleAsync(
            id, request.Name, request.Icon, request.IsImmutable, request.IsSystem, request.Permissions, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>Delete a role</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRole(Guid id, CancellationToken cancellationToken)
    {
        var result = await _roleService.DeleteRoleAsync(id, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>Assign permissions to a role</summary>
    [HttpPost("{id:guid}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddPermissions(Guid id, [FromBody] List<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var result = await _roleService.AddPermissionsToRoleAsync(id, permissionIds, cancellationToken);
        return OkResponse(result);
    }

    /// <summary>Remove permissions from a role</summary>
    [HttpDelete("{id:guid}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemovePermissions(Guid id, [FromBody] List<Guid> permissionIds, CancellationToken cancellationToken)
    {
        var result = await _roleService.RemovePermissionsFromRoleAsync(id, permissionIds, cancellationToken);
        return OkResponse(result);
    }
}
