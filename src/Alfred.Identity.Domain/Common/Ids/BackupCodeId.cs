namespace Alfred.Identity.Domain.Common.Ids;

public readonly record struct BackupCodeId(Guid Value)
{
    public static BackupCodeId New()
    {
        return new BackupCodeId(Guid.CreateVersion7());
    }

    public static readonly BackupCodeId Empty = new(Guid.Empty);

    public override string ToString()
    {
        return Value.ToString();
    }

    public static implicit operator Guid(BackupCodeId id)
    {
        return id.Value;
    }

    public static implicit operator BackupCodeId(Guid value)
    {
        return new BackupCodeId(value);
    }
}
