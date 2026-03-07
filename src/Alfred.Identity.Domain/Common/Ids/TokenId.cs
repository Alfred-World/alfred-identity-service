namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct TokenId(Guid Value)
{
    public static TokenId New()
    {
        return new TokenId(Guid.CreateVersion7());
    }

    public static readonly TokenId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(TokenId id)
    {
        return id.Value;
    }

    public static implicit operator TokenId(Guid value)
    {
        return new TokenId(value);
    }
}
