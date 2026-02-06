using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Roles.Commands.AddPermissions;
using Alfred.Identity.Application.Roles.Commands.CreateRole;
using Alfred.Identity.Application.Roles.Commands.DeleteRole;
using Alfred.Identity.Application.Roles.Commands.RemovePermissions;
using Alfred.Identity.Application.Roles.Commands.UpdateRole;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Application.Roles.Queries.GetRoleById;

using Alfred.Identity.Application.Roles.Queries.GetRoles;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("roles")]
public class RolesController : BaseApiController
{
    private readonly IMediator _mediator;

    public RolesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of roles
    /// </summary>
    /// <remarks>
    /// Supports filtering, sorting, and pagination via query parameters.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetRoles(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var query = new GetRolesQuery(queryRequest.ToQueryRequest());
        var result = await _mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result);
    }

    /// <summary>
    /// Get role by ID
    /// </summary>
    /// <param name="id">The unique identifier of the role</param>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetRoleById(Guid id)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id));
        if (result == null)
        {
            return NotFound();
        }

        return OkResponse(result);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    /// <param name="command">Role creation details</param>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequestResponse<RoleDto>(result.Error);
        }

        return CreatedResponse(result.Data);
    }

    /// <summary>
    /// Update an existing role
    /// </summary>
    /// <param name="id">ID of the role to update</param>
    /// <param name="command">Role update details</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> UpdateRole(Guid id, [FromBody] UpdateRoleCommand command)
    {
        if (id != command.Id)
        {
            return BadRequestResponse<RoleDto>("ID mismatch");
        }

        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequestResponse<RoleDto>(result.Error);
        }

        return OkResponse(result.Data);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="id">ID of the role to delete</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> DeleteRole(Guid id)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(id));
        if (!result.Success)
        {
            return BadRequestResponse<RoleDto>(result.Error);
        }

        return OkResponse(result.Data);
    }

    /// <summary>
    /// Assign permissions to a role
    /// </summary>
    /// <param name="id">ID of the role</param>
    /// <param name="permissionIds">List of permission IDs to assign</param>
    [HttpPost("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<AddPermissionsToRoleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AddPermissionsToRoleResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddPermissionsToRoleResult>> AddPermissions(Guid id,
        [FromBody] List<Guid> permissionIds)
    {
        var result = await _mediator.Send(new AddPermissionsToRoleCommand(id, permissionIds));
        if (!result.Success)
        {
            return BadRequestResponse<AddPermissionsToRoleResult>(result.Error);
        }

        return OkResponse(result);
    }

    /// <summary>
    /// Remove permissions from a role
    /// </summary>
    /// <param name="id">ID of the role</param>
    /// <param name="permissionIds">List of permission IDs to remove</param>
    [HttpDelete("{id}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<RemovePermissionsFromRoleResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RemovePermissionsFromRoleResult>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemovePermissionsFromRoleResult>> RemovePermissions(Guid id,
        [FromBody] List<Guid> permissionIds)
    {
        var result = await _mediator.Send(new RemovePermissionsFromRoleCommand(id, permissionIds));
        if (!result.Success)
        {
            return BadRequestResponse<RemovePermissionsFromRoleResult>(result.Error);
        }

        return OkResponse(result);
    }
}
