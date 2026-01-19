using System.Text.Encodings.Web;

namespace Alfred.Identity.Application.Querying.ErrorHandling;

/// <summary>
/// Error response cho filter operations với sanitized messages
/// </summary>
public sealed record FilterErrorResponse
{
    /// <summary>
    /// User-friendly error message
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Detailed error information (HTML encoded để prevent XSS)
    /// Null nếu không muốn leak thông tin trong production
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Error code để client side tracking
    /// </summary>
    public int ErrorCode { get; init; }

    /// <summary>
    /// Position trong filter string nơi error xảy ra (nếu có)
    /// </summary>
    public int? Position { get; init; }
}

/// <summary>
/// Helper class để handle filter errors với HTML encoding (prevent XSS)
/// </summary>
public static class FilterErrorHandler
{
    private static readonly HtmlEncoder _htmlEncoder = HtmlEncoder.Default;

    /// <summary>
    /// Handle parser errors - syntax issues
    /// Error Code: 1001
    /// </summary>
    public static FilterErrorResponse HandleParseError(FormatException ex)
    {
        var message = ExtractPosition(ex.Message, out var position);

        return new FilterErrorResponse
        {
            Message = "Invalid filter syntax",
            Details = _htmlEncoder.Encode(message),
            ErrorCode = 1001,
            Position = position
        };
    }

    /// <summary>
    /// Handle validation errors - invalid operators, fields, types
    /// Error Code: 1002
    /// </summary>
    public static FilterErrorResponse HandleValidationError(InvalidOperationException ex)
    {
        return new FilterErrorResponse
        {
            Message = "Filter validation failed",
            Details = _htmlEncoder.Encode(ex.Message),
            ErrorCode = 1002
        };
    }

    /// <summary>
    /// Handle argument errors - invalid input
    /// Error Code: 1003
    /// </summary>
    public static FilterErrorResponse HandleArgumentError(ArgumentException ex)
    {
        return new FilterErrorResponse
        {
            Message = "Invalid filter argument",
            Details = _htmlEncoder.Encode(ex.Message),
            ErrorCode = 1003
        };
    }

    /// <summary>
    /// Handle not supported operations
    /// Error Code: 1004
    /// </summary>
    public static FilterErrorResponse HandleNotSupportedError(NotSupportedException ex)
    {
        return new FilterErrorResponse
        {
            Message = "Operation not supported",
            Details = _htmlEncoder.Encode(ex.Message),
            ErrorCode = 1004
        };
    }

    /// <summary>
    /// Handle unexpected errors - generic fallback
    /// Error Code: 5000
    /// Note: Chi tiết không được return trong production
    /// </summary>
    public static FilterErrorResponse HandleUnexpectedError(Exception ex, bool includeDetails = false)
    {
        return new FilterErrorResponse
        {
            Message = "An error occurred while processing your filter",
            Details = includeDetails ? _htmlEncoder.Encode(ex.Message) : null,
            ErrorCode = 5000
        };
    }

    /// <summary>
    /// Extract position info từ error message nếu có
    /// </summary>
    private static string ExtractPosition(string message, out int? position)
    {
        position = null;

        // Try to extract position from message like "at position 42"
        if (message.Contains("at position"))
        {
            var parts = message.Split("position");
            if (parts.Length > 1)
            {
                var positionPart = parts[1].Trim();
                if (int.TryParse(positionPart.Split(' ', ',')[0], out var pos))
                {
                    position = pos;
                }
            }
        }

        return message;
    }

    /// <summary>
    /// Safely encode string for HTML context (prevent XSS)
    /// </summary>
    public static string EncodeForHtml(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        return _htmlEncoder.Encode(text);
    }

    /// <summary>
    /// Create error response from any exception
    /// </summary>
    public static FilterErrorResponse FromException(Exception ex, bool includeDetails = false)
    {
        return ex switch
        {
            FormatException fe => HandleParseError(fe),
            InvalidOperationException ioe => HandleValidationError(ioe),
            ArgumentException ae => HandleArgumentError(ae),
            NotSupportedException nse => HandleNotSupportedError(nse),
            _ => HandleUnexpectedError(ex, includeDetails)
        };
    }
}
