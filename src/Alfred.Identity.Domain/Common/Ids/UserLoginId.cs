namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct UserLoginId(Guid Value)
{
    public static UserLoginId New()
    {
        return new UserLoginId(Guid.CreateVersion7());
    }

    public static readonly UserLoginId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(UserLoginId id)
    {
        return id.Value;
    }

    public static implicit operator UserLoginId(Guid value)
    {
        return new UserLoginId(value);
    }
}
