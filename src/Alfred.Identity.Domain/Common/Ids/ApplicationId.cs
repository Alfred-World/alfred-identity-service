namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct ApplicationId(Guid Value)
{
    public static ApplicationId New()
    {
        return new ApplicationId(Guid.CreateVersion7());
    }

    public static readonly ApplicationId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(ApplicationId id)
    {
        return id.Value;
    }

    public static implicit operator ApplicationId(Guid value)
    {
        return new ApplicationId(value);
    }
}
