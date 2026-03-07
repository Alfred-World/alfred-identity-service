namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct SigningKeyId(Guid Value)
{
    public static SigningKeyId New()
    {
        return new SigningKeyId(Guid.CreateVersion7());
    }

    public static readonly SigningKeyId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(SigningKeyId id)
    {
        return id.Value;
    }

    public static implicit operator SigningKeyId(Guid value)
    {
        return new SigningKeyId(value);
    }
}
