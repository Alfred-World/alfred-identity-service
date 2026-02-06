using Alfred.Identity.Application.Querying.Core;

using Swashbuckle.AspNetCore.Annotations;

namespace Alfred.Identity.WebApi.Contracts.Common;

/// <summary>
/// Unified API response wrapper for all API responses (success + error).
/// - On success: Success=true, Result is populated, Errors is null.
/// - On failure: Success=false, Errors is populated, Result is null.
///
/// This enables discriminated union pattern on the frontend:
///   if (data.success) { data.result... } else { data.errors... }
/// </summary>
[SwaggerSchema(Required = ["success"])]
public sealed record ApiResponse<TResult>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public TResult? Result { get; init; }
    public List<ApiError>? Errors { get; init; }

    // --- Success factory methods ---
    public static ApiResponse<TResult> Ok(TResult? result, string? message = null)
        => new() { Success = true, Message = message, Result = result };

    public static ApiResponse<TResult> Ok(TResult? result, string? message, params object?[] args)
        => new()
        {
            Success = true,
            Message = message != null ? string.Format(message, args) : null,
            Result = result
        };

    // --- Error factory methods ---
    public static ApiResponse<TResult> Fail(string message, string code = "BAD_REQUEST")
        => new() { Success = false, Errors = [new ApiError(message, code)] };

    public static ApiResponse<TResult> Fail(List<ApiError> errors)
        => new() { Success = false, Errors = errors };

    public static ApiResponse<TResult> BadRequest(string message, string code = "BAD_REQUEST")
        => Fail(message, code);

    public static ApiResponse<TResult> Unauthorized(string message, string code = "UNAUTHORIZED")
        => Fail(message, code);

    public static ApiResponse<TResult> Forbidden(string message, string code = "FORBIDDEN")
        => Fail(message, code);

    public static ApiResponse<TResult> NotFound(string message, string code = "NOT_FOUND")
        => Fail(message, code);

    public static ApiResponse<TResult> InternalServerError(
        string message = "An internal server error occurred",
        string code = "INTERNAL_SERVER_ERROR")
        => Fail(message, code);

    public static ApiResponse<TResult> ValidationError(params ApiError[] errors)
        => new() { Success = false, Errors = errors.ToList() };

    public static ApiResponse<TResult> From(Exception ex, string code = "INTERNAL_SERVER_ERROR")
        => new() { Success = false, Errors = [new ApiError(ex.Message, code)] };
}

/// <summary>
/// Non-generic API error response for use when no result type is needed.
/// Convenience type for error-only responses (e.g. global exception handler).
/// </summary>
[SwaggerSchema(Required = ["success", "errors"])]
public sealed record ApiErrorResponse(
    bool Success,
    List<ApiError> Errors
)
{
    public static ApiErrorResponse BadRequest(string message, string code = "BAD_REQUEST")
        => new(false, [new ApiError(message, code)]);

    public static ApiErrorResponse Unauthorized(string message, string code = "UNAUTHORIZED")
        => new(false, [new ApiError(message, code)]);

    public static ApiErrorResponse Forbidden(string message, string code = "FORBIDDEN")
        => new(false, [new ApiError(message, code)]);

    public static ApiErrorResponse NotFound(string message, string code = "NOT_FOUND")
        => new(false, [new ApiError(message, code)]);

    public static ApiErrorResponse InternalServerError(
        string message = "An internal server error occurred",
        string code = "INTERNAL_SERVER_ERROR")
        => new(false, [new ApiError(message, code)]);

    public static ApiErrorResponse ValidationError(params ApiError[] errors)
        => new(false, errors.ToList());

    public static ApiErrorResponse From(Exception ex, string code = "INTERNAL_SERVER_ERROR")
        => new(false, [new ApiError(ex.Message, code)]);
}

/// <summary>
/// Error detail with message and i18n code
/// </summary>
[SwaggerSchema(Required = ["message", "code"])]
public sealed record ApiError(
    string Message,
    string Code
);

/// <summary>
/// Paginated API response (unified with error support).
/// Same discriminated union pattern as ApiResponse.
/// </summary>
[SwaggerSchema(Required = ["success"])]
public sealed record ApiPagedResponse<T>
{
    public bool Success { get; init; }
    public string? Message { get; init; }
    public PageResult<T>? Result { get; init; }
    public List<ApiError>? Errors { get; init; }

    public static ApiPagedResponse<T> Ok(PageResult<T> result, string? message = null)
        => new() { Success = true, Message = message, Result = result };

    public static ApiPagedResponse<T> Fail(string message, string code = "BAD_REQUEST")
        => new() { Success = false, Errors = [new ApiError(message, code)] };
}
