using System.Text.Json;
using System.Text.Json.Serialization;

using Alfred.Identity.Application.Common.Exceptions;
using Alfred.Identity.Domain.Common.Exceptions;
using Alfred.Identity.WebApi.Contracts.Common;

using Microsoft.AspNetCore.Diagnostics;

namespace Alfred.Identity.WebApi.Middleware;

/// <summary>
/// Global exception handler - handles Domain exceptions and unexpected errors.
/// Returns standardized {success, errors} or {success, result} format.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, errorResponse) = exception switch
        {
            FilterValidationException filterEx => HandleFilterValidationException(filterEx),
            DomainException domainEx => HandleDomainException(domainEx),
            UnauthorizedAccessException => HandleUnauthorizedAccessException(),
            KeyNotFoundException keyNotFoundEx => HandleKeyNotFoundException(keyNotFoundEx),
            InvalidOperationException invalidOpEx => HandleInvalidOperationException(invalidOpEx),
            ArgumentException argumentEx => HandleArgumentException(argumentEx),
            _ => HandleGenericException(exception)
        };

        // Only log server errors (5xx), not client errors (4xx)
        if (statusCode >= 500)
        {
            _logger.LogError(exception,
                "Server error: {ExceptionType} - {ExceptionMessage}",
                exception.GetType().Name, exception.Message);
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

    private static (int statusCode, ApiErrorResponse response) HandleInvalidOperationException(
        InvalidOperationException ex)
    {
        return (StatusCodes.Status400BadRequest,
            ApiErrorResponse.BadRequest(ex.Message, "INVALID_OPERATION"));
    }

    private static (int statusCode, ApiErrorResponse response) HandleArgumentException(ArgumentException ex)
    {
        return (StatusCodes.Status400BadRequest,
            ApiErrorResponse.BadRequest(ex.Message, "INVALID_ARGUMENT"));
    }

    private static (int statusCode, ApiErrorResponse response) HandleGenericException(Exception ex)
    {
        return (StatusCodes.Status500InternalServerError,
            ApiErrorResponse.InternalServerError(
                "An internal server error occurred",
                "INTERNAL_SERVER_ERROR"));
    }
}
