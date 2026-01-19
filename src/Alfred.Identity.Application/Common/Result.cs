namespace Alfred.Identity.Application.Common;

/// <summary>
/// Error types for categorizing failures per RFC 7231 / RFC 4918
/// </summary>
public enum ErrorType
{
    /// <summary>400 - Malformed request syntax</summary>
    BadRequest,

    /// <summary>401 - Authentication required</summary>
    Unauthorized,

    /// <summary>403 - Access denied</summary>
    Forbidden,

    /// <summary>404 - Resource not found</summary>
    NotFound,

    /// <summary>409 - Resource conflict (duplicate, state conflict)</summary>
    Conflict,

    /// <summary>422 - Semantically incorrect (business/domain validation failed)</summary>
    UnprocessableEntity,

    /// <summary>400 - Legacy alias for BadRequest</summary>
    Validation = BadRequest
}

/// <summary>
/// Generic Result wrapper for handling success/failure cases
/// </summary>
public class Result<T>
{
    private Result(bool isSuccess, T? value, string? error, ErrorType? errorType)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorType = errorType;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string? Error { get; }
    public ErrorType? ErrorType { get; }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, null, null);
    }

    public static Result<T> Failure(string error, ErrorType errorType = Common.ErrorType.Validation)
    {
        return new Result<T>(false, default, error, errorType);
    }

    /// <summary>
    /// Match pattern for handling both success and failure cases
    /// </summary>
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }

    /// <summary>
    /// Extended Match pattern with error type information
    /// </summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, ErrorType, TResult> onFailure)
    {
        return IsSuccess
            ? onSuccess(Value!)
            : onFailure(Error!, ErrorType ?? Common.ErrorType.Validation);
    }
}
