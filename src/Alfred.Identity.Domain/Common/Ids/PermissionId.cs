namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct PermissionId(Guid Value)
{
    public static PermissionId New()
    {
        return new PermissionId(Guid.CreateVersion7());
    }

    public static readonly PermissionId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(PermissionId id)
    {
        return id.Value;
    }

    public static implicit operator PermissionId(Guid value)
    {
        return new PermissionId(value);
    }
}
