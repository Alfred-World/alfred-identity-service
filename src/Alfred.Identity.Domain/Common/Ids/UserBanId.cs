namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct UserBanId(Guid Value)
{
    public static UserBanId New()
    {
        return new UserBanId(Guid.CreateVersion7());
    }

    public static readonly UserBanId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(UserBanId id)
    {
        return id.Value;
    }

    public static implicit operator UserBanId(Guid value)
    {
        return new UserBanId(value);
    }
}
