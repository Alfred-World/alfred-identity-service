using Alfred.Identity.Application.Querying;

using Swashbuckle.AspNetCore.Annotations;

namespace Alfred.Identity.WebApi.Contracts.Common;

/// <summary>
/// Standard API response wrapper for successful responses
/// </summary>
[SwaggerSchema(Required = new[] { "success" })]
public sealed record ApiSuccessResponse<TResult>(
    bool Success,
    string? Message,
    TResult? Result
)
{
    public static ApiSuccessResponse<TResult> Ok(TResult? result, string? message = null)
    {
        return new ApiSuccessResponse<TResult>(true, message, result);
    }

    public static ApiSuccessResponse<TResult> Ok(TResult? result, string? message, params object?[] args)
    {
        return new ApiSuccessResponse<TResult>(true, message != null ? string.Format(message, args) : null, result);
    }
}

/// <summary>
/// Standard API response wrapper for error responses
/// </summary>
[SwaggerSchema(Required = new[] { "success", "errors" })]
public sealed record ApiErrorResponse(
    bool Success,
    List<ApiError> Errors
)
{
    public static ApiErrorResponse BadRequest(string message, string code = "BAD_REQUEST")
    {
        return new ApiErrorResponse(false, new List<ApiError> { new(message, code) });
    }

    public static ApiErrorResponse BadRequest(params ApiError[] errors)
    {
        return new ApiErrorResponse(false, errors.ToList());
    }

    public static ApiErrorResponse Unauthorized(string message, string code = "UNAUTHORIZED")
    {
        return new ApiErrorResponse(false, new List<ApiError> { new(message, code) });
    }

    public static ApiErrorResponse Forbidden(string message, string code = "FORBIDDEN")
    {
        return new ApiErrorResponse(false, new List<ApiError> { new(message, code) });
    }

    public static ApiErrorResponse NotFound(string message, string code = "NOT_FOUND")
    {
        return new ApiErrorResponse(false, new List<ApiError> { new(message, code) });
    }

    public static ApiErrorResponse InternalServerError(string message = "An internal server error occurred",
        string code = "INTERNAL_SERVER_ERROR")
    {
        return new ApiErrorResponse(false, new List<ApiError> { new(message, code) });
    }

    public static ApiErrorResponse ValidationError(params ApiError[] errors)
    {
        return new ApiErrorResponse(false, errors.ToList());
    }

    public static ApiErrorResponse From(Exception ex, string code = "INTERNAL_SERVER_ERROR")
    {
        return new ApiErrorResponse(false, new List<ApiError> { new(ex.Message, code) });
    }
}

/// <summary>
/// Error detail with message and i18n code
/// </summary>
[SwaggerSchema(Required = new[] { "message", "code" })]
public sealed record ApiError(
    string Message,
    string Code
);

/// <summary>
/// Paginated API response
/// </summary>
public sealed record ApiPagedResponse<T>(
    bool Success,
    string? Message,
    PageResult<T> Result
)
{
    public static ApiPagedResponse<T> Ok(PageResult<T> result, string? message = null)
    {
        return new ApiPagedResponse<T>(true, message, result);
    }
}
