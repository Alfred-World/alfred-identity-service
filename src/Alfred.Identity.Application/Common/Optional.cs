namespace Alfred.Identity.Application.Common;

/// <summary>
/// Represents a value that may or may not have been provided in a partial update (PATCH) request.
/// <para>
/// Distinguishes three states:
/// <list type="bullet">
///   <item><description>Not sent (HasValue = false) — keep existing value in database</description></item>
///   <item><description>Sent as null (HasValue = true, Value = default) — set database value to null</description></item>
///   <item><description>Sent with value (HasValue = true, Value = T) — update database value</description></item>
/// </list>
/// </para>
/// </summary>
public readonly struct Optional<T>
{
    private readonly T? _value;

    /// <summary>Whether a value was explicitly provided (including null).</summary>
    public bool HasValue { get; }

    /// <summary>The provided value. Throws if HasValue is false.</summary>
    public T Value => HasValue
        ? _value!
        : throw new InvalidOperationException("No value was provided for this optional field.");

    private Optional(T? value)
    {
        _value = value;
        HasValue = true;
    }

    /// <summary>Creates an Optional with an explicit value (marks as provided).</summary>
    public static Optional<T> Of(T? value)
    {
        return new Optional<T>(value);
    }

    /// <summary>Returns the provided value if present, otherwise the fallback (existing entity value).</summary>
    public T GetValueOrDefault(T fallback)
    {
        return HasValue ? _value! : fallback;
    }

    /// <summary>Maps the inner value to a different type, preserving the provided/not-provided state.</summary>
    public Optional<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        return HasValue ? Optional<TResult>.Of(selector(_value!)) : default;
    }

    public static implicit operator Optional<T>(T value)
    {
        return Of(value);
    }

    public override string ToString()
    {
        return HasValue ? _value?.ToString() ?? "null" : "(not provided)";
    }
}
