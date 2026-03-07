namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct UserActivityLogId(Guid Value)
{
    public static UserActivityLogId New()
    {
        return new UserActivityLogId(Guid.CreateVersion7());
    }

    public static readonly UserActivityLogId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(UserActivityLogId id)
    {
        return id.Value;
    }

    public static implicit operator UserActivityLogId(Guid value)
    {
        return new UserActivityLogId(value);
    }
}
