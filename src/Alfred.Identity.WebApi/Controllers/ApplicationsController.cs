using Alfred.Identity.Application.Applications.Commands.Delete;
using Alfred.Identity.Application.Applications.Commands.RegenerateSecret;
using Alfred.Identity.Application.Applications.Commands.UpdateStatus;
using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.Application.Applications.Queries.GetApplicationById;
using Alfred.Identity.Application.Applications.Queries.GetApplications;
using Alfred.Identity.Application.Applications.Queries.GetMetadata;
using Alfred.Identity.WebApi.Contracts.Applications;
using Alfred.Identity.WebApi.Contracts.Common;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("applications")]
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
    /// Get application metadata (types, permissions)
    /// </summary>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(ApiSuccessResponse<ApplicationMetadataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadata(CancellationToken cancellationToken)
    {
        var query = new GetApplicationMetadataQuery();
        var result = await _mediator.Send(query, cancellationToken);
        return OkResponse(result.Value);
    }

    /// <summary>
    /// Get application by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
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
        var createResult = await _mediator.Send(command, cancellationToken);
        var id = createResult.Id;

        // Get the created application to return as DTO
        var getQuery = new GetApplicationByIdQuery(id);
        var queryResult = await _mediator.Send(getQuery, cancellationToken);

        var appDto = queryResult.Value!;

        // Enrich DTO with the secret if available
        if (!string.IsNullOrEmpty(createResult.Secret))
        {
            appDto = appDto with { ClientSecret = createResult.Secret };
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id },
            ApiSuccessResponse<ApplicationDto>.Ok(appDto, "Application created successfully")
        );
    }

    /// <summary>
    /// Update an existing application
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
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
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteApplicationCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            return NotFoundResponse(result.Error ?? "Application not found");
        }

        return OkResponse(true, "Application deleted successfully");
    }

    /// <summary>
    /// Update application status (activate/deactivate)
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiSuccessResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateApplicationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateApplicationStatusCommand(id, request.IsActive);
        var result = await _mediator.Send(command, cancellationToken);

        if (!result)
        {
            return NotFoundResponse("Application not found");
        }

        return OkResponse(true, "Application status updated successfully");
    }

    /// <summary>
    /// Regenerate client secret (returns the new raw secret)
    /// </summary>
    [HttpPost("{id:guid}/secret/regenerate")]
    [ProducesResponseType(typeof(ApiSuccessResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var command = new RegenerateClientSecretCommand(id);
        var result = await _mediator.Send(command, cancellationToken);

        if (result == null)
        {
            return NotFoundResponse("Application not found");
        }

        return OkResponse(result, "Client secret regenerated successfully. Please save it immediately.");
    }
}
