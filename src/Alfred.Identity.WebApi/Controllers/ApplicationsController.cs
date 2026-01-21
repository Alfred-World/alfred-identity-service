using Alfred.Identity.Application.Applications.Commands.Delete;
using Alfred.Identity.Application.Applications.Queries.GetApplicationById;
using Alfred.Identity.Application.Applications.Queries.GetApplications;
using Alfred.Identity.Application.Applications.Shared;
using Alfred.Identity.WebApi.Contracts.Applications;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[ApiController]
[Route("applications")]
[Produces("application/json")]
// [Authorize(Roles = "Admin")] // Uncomment when roles are set up
public class ApplicationsController : BaseApiController
{
    private readonly IMediator _mediator;

    public ApplicationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get paginated list of applications
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var query = new GetApplicationsQuery(queryRequest.ToQueryRequest());
        var result = await _mediator.Send(query, cancellationToken);
        return OkPaginatedResponse(result);
    }

    /// <summary>
    /// Get application by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var query = new GetApplicationByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result.IsFailure)
        {
            return NotFoundResponse(result.Error ?? "Application not found");
        }

        return OkResponse(result.Value);
    }

    /// <summary>
    /// Create a new OAuth2 client application
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiSuccessResponse<ApplicationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToCreateCommand();
        var id = await _mediator.Send(command, cancellationToken);

        // Get the created application to return as DTO
        var getQuery = new GetApplicationByIdQuery(id);
        var result = await _mediator.Send(getQuery, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            ApiSuccessResponse<ApplicationDto>.Ok(result.Value, "Application created successfully")
        );
    }

    /// <summary>
    /// Update an existing application
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        long id,
        [FromBody] UpdateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = request.ToUpdateCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error!.Contains("not found")
                ? NotFoundResponse(result.Error)
                : BadRequestResponse(result.Error ?? "Failed to update application");
        }

        return OkResponse(result.Value, "Application updated successfully");
    }

    /// <summary>
    /// Delete an application
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        var command = new DeleteApplicationCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFoundResponse(result.Error ?? "Application not found");
        }

        return OkResponse(true, "Application deleted successfully");
    }
}
