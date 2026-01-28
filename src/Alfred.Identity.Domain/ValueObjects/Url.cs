using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Exceptions;

namespace Alfred.Identity.Domain.ValueObjects;

/// <summary>
/// Value Object for generic HTTP/HTTPS URL
/// </summary>
public sealed class Url : ValueObject
{
    public const int MaxLength = 500;

    public string Value { get; private set; } = string.Empty;

    #region Constructors

    // Private constructor for EF Core
    private Url()
    {
    }

    private Url(string value)
    {
        Value = value;
    }

    #endregion

    #region Factory Methods

    public static Url Create(string? urlString)
    {
        if (string.IsNullOrWhiteSpace(urlString))
        {
            return new Url(string.Empty);
        }

        var sanitizedUrl = urlString.Trim();

        if (sanitizedUrl.Length > MaxLength)
        {
            throw new DomainException($"URL cannot exceed {MaxLength} characters.");
        }

        if (!IsValidUrl(sanitizedUrl))
        {
            throw new DomainException("URL must be a valid HTTP or HTTPS address.");
        }

        return new Url(sanitizedUrl);
    }

    public static Url Empty()
    {
        return new Url(string.Empty);
    }

    #endregion

    #region Business Methods

    public bool IsEmpty()
    {
        return string.IsNullOrWhiteSpace(Value);
    }

    public bool IsHttps()
    {
        if (IsEmpty())
        {
            return false;
        }

        return Uri.TryCreate(Value, UriKind.Absolute, out var uriResult)
               && uriResult.Scheme == Uri.UriSchemeHttps;
    }

    #endregion

    #region Helper Methods

    private static bool IsValidUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttp ||
                   absoluteUri.Scheme == Uri.UriSchemeHttps;
        }

        if (Uri.TryCreate(url, UriKind.Relative, out var relativeUri))
        {
            return url.StartsWith("/");
        }

        return false;
    }

    #endregion

    #region ValueObject Overrides

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(Url url)
    {
        return url.Value;
    }

    public static explicit operator Url(string urlString)
    {
        return Create(urlString);
    }

    #endregion
}
