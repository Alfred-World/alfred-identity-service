using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Roles.Commands.AddPermissions;
using Alfred.Identity.Application.Roles.Commands.CreateRole;
using Alfred.Identity.Application.Roles.Commands.DeleteRole;
using Alfred.Identity.Application.Roles.Commands.RemovePermissions;
using Alfred.Identity.Application.Roles.Commands.UpdateRole;
using Alfred.Identity.Application.Roles.Common;
using Alfred.Identity.Application.Roles.Queries.GetRoleById;
using Alfred.Identity.Application.Roles.Queries.GetRolePermissions;
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
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> GetRoleById(long id)
    {
        var result = await _mediator.Send(new GetRoleByIdQuery(id));
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    /// <param name="command">Role creation details</param>
    [HttpPost]
    [ProducesResponseType(typeof(CreateRoleResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateRoleResult>> CreateRole([FromBody] CreateRoleCommand command)
    {
        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetRoleById), new { id = result.RoleId }, result);
    }

    /// <summary>
    /// Update an existing role
    /// </summary>
    /// <param name="id">ID of the role to update</param>
    /// <param name="command">Role update details</param>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UpdateRoleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UpdateRoleResult>> UpdateRole(long id, [FromBody] UpdateRoleCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("ID mismatch");
        }

        var result = await _mediator.Send(command);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    /// <param name="id">ID of the role to delete</param>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(DeleteRoleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteRoleResult>> DeleteRole(long id)
    {
        var result = await _mediator.Send(new DeleteRoleCommand(id));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Get permissions assigned to a role
    /// </summary>
    /// <param name="id">ID of the role</param>
    [HttpGet("{id}/permissions")]
    [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<PermissionDto>>> GetRolePermissions(long id)
    {
        var result = await _mediator.Send(new GetRolePermissionsQuery(id));
        return Ok(result);
    }

    /// <summary>
    /// Assign permissions to a role
    /// </summary>
    /// <param name="id">ID of the role</param>
    /// <param name="permissionIds">List of permission IDs to assign</param>
    [HttpPost("{id}/permissions")]
    [ProducesResponseType(typeof(AddPermissionsToRoleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AddPermissionsToRoleResult>> AddPermissions(long id,
        [FromBody] List<long> permissionIds)
    {
        var result = await _mediator.Send(new AddPermissionsToRoleCommand(id, permissionIds));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Remove permissions from a role
    /// </summary>
    /// <param name="id">ID of the role</param>
    /// <param name="permissionIds">List of permission IDs to remove</param>
    [HttpDelete("{id}/permissions")]
    [ProducesResponseType(typeof(RemovePermissionsFromRoleResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RemovePermissionsFromRoleResult>> RemovePermissions(long id,
        [FromBody] List<long> permissionIds)
    {
        var result = await _mediator.Send(new RemovePermissionsFromRoleCommand(id, permissionIds));
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}
