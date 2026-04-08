using System.Text.Json;
using System.Text.Json.Serialization;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Domain.Common.Exceptions;

using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;

namespace Alfred.Identity.WebApi.Middleware;

/// <summary>
/// Global exception handler - handles Domain exceptions and unexpected errors.
/// Returns standardized {success, errors} or {success, result} format.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errorResponse) = exception switch
        {
            ValidationException validationEx => HandleValidationException(validationEx),
            FilterValidationException filterEx => HandleFilterValidationException(filterEx),
            // InvalidOperationException from filter binder must NOT leak internal type/property names
            InvalidOperationException invalidOpEx when IsFilterBinderException(invalidOpEx)
                => (StatusCodes.Status400BadRequest,
                    ApiErrorResponse.BadRequest(invalidOpEx.Message, "INVALID_FILTER")),
            DomainException domainEx => HandleDomainException(domainEx),
            UnauthorizedAccessException => HandleUnauthorizedAccessException(),
            KeyNotFoundException keyNotFoundEx => HandleKeyNotFoundException(keyNotFoundEx),
            InvalidOperationException invalidOpEx => HandleInvalidOperationException(invalidOpEx),
            ArgumentException argumentEx => HandleArgumentException(argumentEx),
            _ => HandleGenericException(exception)
        };

        // Log errors based on environment
        if (statusCode >= 500)
        {
            _logger.LogError(exception,
                "Server error: {ExceptionType} - {ExceptionMessage}",
                exception.GetType().Name, exception.Message);
        }
        else if (_environment.IsDevelopment())
        {
            // In development, log client errors too for debugging
            _logger.LogWarning(exception,
                "Client error ({StatusCode}): {ExceptionType} - {ExceptionMessage}",
                statusCode, exception.GetType().Name, exception.Message);
        }
        // Don't log FilterValidationException - it's just user input validation error

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            }),
            cancellationToken);

        return true;
    }

    private static bool IsFilterBinderException(InvalidOperationException ex)
    {
        // Messages from FilterExpressionBinder and SortExpressionBinder are safe to surface
        // because they describe user-facing filter/field errors (field not filterable/sortable/found).
        // Messages from internal reflection (InnerField reflection, CoerceConstant) may contain entity internals —
        // those are caught here via source check via stack.
        var msg = ex.Message;
        return msg.Contains("not filterable", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("not sortable", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("not found", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("not a collection type", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("requires an inner filter", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("Cannot convert", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("is not a valid value", StringComparison.OrdinalIgnoreCase);
    }

    private static (int statusCode, ApiErrorResponse response) HandleValidationException(ValidationException ex)
    {
        var errors = ex.Errors.Select(e => new ApiError(e.ErrorMessage, e.ErrorCode)).ToList();
        return (StatusCodes.Status400BadRequest, new ApiErrorResponse(false, errors));
    }


    private static (int statusCode, ApiErrorResponse response) HandleFilterValidationException(
        FilterValidationException ex)
    {
        // Extract just the first line of the message (the main error)
        var mainMessage = ex.Message?.Split('\n')[0] ?? "Invalid filter";
        return (StatusCodes.Status400BadRequest,
            ApiErrorResponse.BadRequest(mainMessage, "INVALID_FILTER"));
    }

    private static (int statusCode, ApiErrorResponse response) HandleDomainException(DomainException ex)
    {
        return (StatusCodes.Status422UnprocessableEntity,
            ApiErrorResponse.BadRequest(ex.Message, "DOMAIN_ERROR"));
    }

    private static (int statusCode, ApiErrorResponse response) HandleUnauthorizedAccessException()
    {
        return (StatusCodes.Status401Unauthorized,
            ApiErrorResponse.Unauthorized("Unauthorized access", "UNAUTHORIZED"));
    }

    private static (int statusCode, ApiErrorResponse response) HandleKeyNotFoundException(KeyNotFoundException ex)
    {
        return (StatusCodes.Status404NotFound,
            ApiErrorResponse.NotFound(ex.Message, "NOT_FOUND"));
    }

    private (int statusCode, ApiErrorResponse response) HandleInvalidOperationException(
        InvalidOperationException ex)
    {
        var message = ex.Message;

        // In development, include inner exception details
        if (_environment.IsDevelopment() && ex.InnerException != null)
        {
            message += $" → {ex.InnerException.Message}";
        }

        return (StatusCodes.Status400BadRequest,
            ApiErrorResponse.BadRequest(message, "INVALID_OPERATION"));
    }

    private (int statusCode, ApiErrorResponse response) HandleArgumentException(ArgumentException ex)
    {
        var message = ex.Message;

        // In development, include parameter name and inner exception details
        if (_environment.IsDevelopment() && !string.IsNullOrEmpty(ex.ParamName))
        {
            message = $"[{ex.ParamName}] {message}";

            if (ex.InnerException != null)
            {
                message += $" → {ex.InnerException.Message}";
            }
        }

        return (StatusCodes.Status400BadRequest,
            ApiErrorResponse.BadRequest(message, "INVALID_ARGUMENT"));
    }

    private static (int statusCode, ApiErrorResponse response) HandleGenericException(Exception ex)
    {
        return (StatusCodes.Status500InternalServerError,
            ApiErrorResponse.InternalServerError(
                "An internal server error occurred",
                "INTERNAL_SERVER_ERROR"));
    }
}
