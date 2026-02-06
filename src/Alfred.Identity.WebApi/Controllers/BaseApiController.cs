using System.Security.Claims;

using Alfred.Identity.Application.Querying.Core;
using Alfred.Identity.WebApi.Contracts.Common;

using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;

namespace Alfred.Identity.WebApi.Controllers;

/// <summary>
/// Base API controller with common functionality for all controllers
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")] // Fallback, but specific controllers should override
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Get the current authenticated user's ID from JWT claims
    /// </summary>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">If user ID is not found in token</exception>
    protected long GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }

        return userId;
    }

    /// <summary>
    /// Try to get the current authenticated user's ID from JWT claims
    /// </summary>
    /// <returns>User ID if found and valid, null otherwise</returns>
    protected long? TryGetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        return long.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    /// <summary>
    /// Get the client's IP address from request headers or connection
    /// </summary>
    /// <returns>Client IP address</returns>
    protected string GetClientIpAddress()
    {
        // Priority order for getting real client IP:
        // 1. CF-Connecting-IP (Cloudflare)
        // 2. X-Forwarded-For (behind proxy/load balancer)
        // 3. X-Real-IP (nginx)
        // 4. RemoteIpAddress (direct connection)

        var cfConnectingIp = Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfConnectingIp))
        {
            return cfConnectingIp;
        }

        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (ips.Length > 0)
            {
                return ips[0].Trim();
            }
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    /// <summary>
    /// Get the user agent string from request headers
    /// </summary>
    /// <returns>User agent string</returns>
    protected string GetUserAgent()
    {
        return Request.Headers.UserAgent.FirstOrDefault() ?? "Unknown";
    }

    #region Response Helpers

    /// <summary>
    /// Return a successful response with data (unified ApiResponse)
    /// </summary>
    protected OkObjectResult OkResponse<T>(T data, string? message = null)
    {
        return Ok(ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// Return a successful response without data
    /// </summary>
    protected OkObjectResult OkResponse(string? message = null)
    {
        return Ok(ApiResponse<object>.Ok(null, message));
    }

    /// <summary>
    /// Return a paginated successful response
    /// </summary>
    protected OkObjectResult OkPaginatedResponse<T>(PageResult<T> result, string? message = null)
    {
        return Ok(ApiPagedResponse<T>.Ok(result, message));
    }

    /// <summary>
    /// Return a created successful response
    /// </summary>
    protected ObjectResult CreatedResponse<T>(T data, string? message = null)
    {
        return StatusCode(201, ApiResponse<T>.Ok(data, message));
    }

    /// <summary>
    /// Return a bad request error response (unified ApiResponse)
    /// </summary>
    protected BadRequestObjectResult BadRequestResponse(string? message, string code = "BAD_REQUEST")
    {
        return BadRequest(ApiResponse<object>.BadRequest(message ?? "Bad Request", code));
    }

    /// <summary>
    /// Return a bad request error response with typed result (for swagger type inference)
    /// </summary>
    protected BadRequestObjectResult BadRequestResponse<T>(string? message, string code = "BAD_REQUEST")
    {
        return BadRequest(ApiResponse<T>.BadRequest(message ?? "Bad Request", code));
    }

    /// <summary>
    /// Return an unauthorized error response
    /// </summary>
    protected UnauthorizedObjectResult UnauthorizedResponse(string message = "Unauthorized",
        string code = "UNAUTHORIZED")
    {
        return Unauthorized(ApiErrorResponse.Unauthorized(message, code));
    }

    /// <summary>
    /// Return a forbidden error response
    /// </summary>
    protected ObjectResult ForbiddenResponse(string message, string code = "FORBIDDEN")
    {
        return StatusCode(403, ApiErrorResponse.Forbidden(message, code));
    }

    /// <summary>
    /// Return a not found error response
    /// </summary>
    protected NotFoundObjectResult NotFoundResponse(string message, string code = "NOT_FOUND")
    {
        return NotFound(ApiErrorResponse.NotFound(message, code));
    }

    /// <summary>
    /// Return a validation error response
    /// </summary>
    protected BadRequestObjectResult ValidationErrorResponse(params ApiError[] errors)
    {
        return BadRequest(ApiResponse<object>.ValidationError(errors));
    }

    /// <summary>
    /// Return an internal server error response
    /// </summary>
    protected ObjectResult InternalErrorResponse(string message = "An internal server error occurred",
        string code = "INTERNAL_SERVER_ERROR")
    {
        return StatusCode(500, ApiErrorResponse.InternalServerError(message, code));
    }

    #endregion
}
