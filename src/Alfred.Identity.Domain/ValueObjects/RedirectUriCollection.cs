using System.Text.Json;

using Alfred.Identity.Domain.Common.Base;
using Alfred.Identity.Domain.Common.Exceptions;

namespace Alfred.Identity.Domain.ValueObjects;

/// <summary>
/// Immutable Value Object representing a validated collection of OAuth2 redirect URIs.
/// Persisted as a JSON array string. Supports any absolute URI scheme (HTTP, HTTPS, custom).
/// </summary>
public sealed class RedirectUriCollection : ValueObject
{
    private readonly HashSet<string> _uris;

    /// <summary>O(1) read-only view of the URI set.</summary>
    public IReadOnlyCollection<string> Uris => _uris;

    /// <summary>JSON array string used for DB persistence.</summary>
    public string Json { get; private set; }

    private RedirectUriCollection(HashSet<string> uris)
    {
        _uris = uris;
        Json = uris.Count == 0 ? "[]" : JsonSerializer.Serialize(uris);
    }

    /// <summary>Returns an empty collection.</summary>
    public static RedirectUriCollection Empty() => new(new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// Create from an enumerable of URI strings.
    /// Validates each URI — any absolute scheme is accepted (https://, http://, myapp://).
    /// Throws <see cref="DomainException"/> on invalid input.
    /// </summary>
    public static RedirectUriCollection Create(IEnumerable<string>? uris)
    {
        if (uris == null)
        {
            return Empty();
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var uri in uris)
        {
            if (string.IsNullOrWhiteSpace(uri))
            {
                continue;
            }

            var trimmed = uri.Trim();

            if (trimmed.Length > 2000)
            {
                throw new DomainException($"Redirect URI exceeds maximum length of 2000 characters: '{trimmed[..50]}...'");
            }

            if (!Uri.TryCreate(trimmed, UriKind.Absolute, out _))
            {
                throw new DomainException($"Redirect URI must be an absolute URI: '{trimmed}'");
            }

            if (set.Count >= 30)
            {
                throw new DomainException("A maximum of 30 redirect URIs are allowed per application.");
            }

            set.Add(trimmed);
        }

        return new RedirectUriCollection(set);
    }

    /// <summary>
    /// Deserialize from a persisted JSON string (or legacy space-delimited format).
    /// Used exclusively by EF Core value converter — never throws.
    /// </summary>
    public static RedirectUriCollection FromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Empty();
        }

        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(json);
            return list == null ? Empty() : Create(list);
        }
        catch (JsonException)
        {
            // Legacy: space-delimited
            var parts = json.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return Create(parts);
        }
        catch (DomainException)
        {
            return Empty();
        }
    }

    /// <summary>True if the collection contains the given URI (case-insensitive).</summary>
    public bool Contains(string uri) => _uris.Contains(uri.Trim());

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Json;
    }

    public override string ToString() => Json;

    /// <summary>Implicit conversion → JSON string for transparent EF/string interop.</summary>
    public static implicit operator string(RedirectUriCollection c) => c.Json;

    /// <summary>Explicit conversion from a JSON or space-delimited string.</summary>
    public static explicit operator RedirectUriCollection(string? s) => FromJson(s);
}
