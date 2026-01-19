namespace Alfred.Identity.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when filter/query validation fails due to invalid user input
/// This is a client error, not a system error
/// </summary>
public class FilterValidationException : InvalidOperationException
{
    public FilterValidationException(string message) : base(message)
    {
    }

    public FilterValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
