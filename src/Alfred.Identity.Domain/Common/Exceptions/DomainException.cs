namespace Alfred.Identity.Domain.Common.Exceptions;

/// <summary>
/// Domain Exception - represents business rule violations.
/// Throws exception directly in English without error codes.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Additional data related to the error (e.g., field name, invalid value).
    /// </summary>
    public IDictionary<string, object>? Details { get; }

    /// <summary>
    /// Create a domain exception with an English message.
    /// </summary>
    public DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Create a domain exception with message and inner exception.
    /// </summary>
    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Create a domain exception with message and additional details.
    /// </summary>
    public DomainException(string message, IDictionary<string, object> details) : base(message)
    {
        Details = details;
    }
}
