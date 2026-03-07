namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct AuthorizationId(Guid Value)
{
    public static AuthorizationId New()
    {
        return new AuthorizationId(Guid.CreateVersion7());
    }

    public static readonly AuthorizationId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(AuthorizationId id)
    {
        return id.Value;
    }

    public static implicit operator AuthorizationId(Guid value)
    {
        return new AuthorizationId(value);
    }
}
