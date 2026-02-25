using Alfred.Identity.Application.Applications;
using Alfred.Identity.Application.Applications.Common;
using Alfred.Identity.WebApi.Contracts.Applications;
using Alfred.Identity.WebApi.Contracts.Common;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

[Route("identity/applications")]
public class ApplicationsController : BaseApiController
{
    private readonly IApplicationService _applicationService;

    public ApplicationsController(IApplicationService applicationService)
    {
        _applicationService = applicationService;
    }

    /// <summary>Get paginated list of applications</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiPagedResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetApplications(
        [FromQuery] PaginationQueryParameters queryRequest,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetAllApplicationsAsync(queryRequest.ToQueryRequest(), cancellationToken);
        return OkPaginatedResponse(result);
    }

    /// <summary>Get application metadata (types, permissions)</summary>
    [HttpGet("metadata")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationMetadataDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetadata(CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetMetadataAsync(cancellationToken);
        return OkResponse(result);
    }

    /// <summary>Get application by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _applicationService.GetApplicationByIdAsync(id, cancellationToken);
        if (result == null)
        {
            return NotFoundResponse("Application not found");
        }

        return OkResponse(result);
    }

    /// <summary>Create a new OAuth2 client application</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.CreateApplicationAsync(
            request.ClientId,
            request.DisplayName,
            request.RedirectUris,
            request.PostLogoutRedirectUris ?? string.Empty,
            request.Permissions ?? string.Empty,
            request.Type,
            cancellationToken);
        return CreatedResponse(result);
    }

    /// <summary>Update an existing application</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _applicationService.UpdateApplicationAsync(
            id,
            request.DisplayName,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.Permissions,
            cancellationToken);
        return OkResponse(result, "Application updated successfully");
    }

    /// <summary>Delete an application</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _applicationService.DeleteApplicationAsync(id, cancellationToken);
        return OkResponse(true, "Application deleted successfully");
    }

    /// <summary>Update application status (activate/deactivate)</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateApplicationStatusRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _applicationService.UpdateStatusAsync(id, request.IsActive, cancellationToken);
        if (!updated)
        {
            return NotFoundResponse("Application not found");
        }

        return OkResponse(true, "Application status updated successfully");
    }

    /// <summary>Regenerate client secret</summary>
    [HttpPost("{id:guid}/secret/regenerate")]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegenerateSecret(Guid id, CancellationToken cancellationToken)
    {
        var rawSecret = await _applicationService.RegenerateClientSecretAsync(id, cancellationToken);
        return OkResponse(rawSecret, "Client secret regenerated successfully. Please save it immediately.");
    }
}
