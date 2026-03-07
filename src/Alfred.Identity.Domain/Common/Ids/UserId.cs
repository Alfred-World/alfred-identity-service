namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct UserId(Guid Value)
{
    public static UserId New()
    {
        return new UserId(Guid.CreateVersion7());
    }

    public static readonly UserId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(UserId id)
    {
        return id.Value;
    }

    public static implicit operator UserId(Guid value)
    {
        return new UserId(value);
    }
}
