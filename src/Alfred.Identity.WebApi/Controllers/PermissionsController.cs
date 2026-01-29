using Alfred.Identity.Application.Permissions.Common;
using Alfred.Identity.Application.Permissions.Queries.GetPermissions;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("permissions")]
public class PermissionsController : BaseApiController
{
    private readonly IMediator _mediator;

    public PermissionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of permissions
    /// </summary>
    /// <remarks>
    /// Supports filtering, sorting, and pagination via query parameters.
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<PermissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPermissions(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var query = new GetPermissionsQuery(queryRequest.ToQueryRequest());
        var result = await _mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result);
    }
}
