namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct ScopeId(Guid Value)
{
    public static ScopeId New()
    {
        return new ScopeId(Guid.CreateVersion7());
    }

    public static readonly ScopeId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(ScopeId id)
    {
        return id.Value;
    }

    public static implicit operator ScopeId(Guid value)
    {
        return new ScopeId(value);
    }
}
