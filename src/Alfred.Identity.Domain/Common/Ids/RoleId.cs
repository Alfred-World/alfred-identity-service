namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct RoleId(Guid Value)
{
    public static RoleId New()
    {
        return new RoleId(Guid.CreateVersion7());
    }

    public static readonly RoleId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(RoleId id)
    {
        return id.Value;
    }

    public static implicit operator RoleId(Guid value)
    {
        return new RoleId(value);
    }
}
