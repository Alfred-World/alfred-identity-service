namespace Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Base entity with GUID primary key using UUID v7 (time-ordered)
/// Inherits all shared logic from BaseEntity<Guid>
/// </summary>
public abstract class BaseEntityGuid : BaseEntity<Guid>
{
    protected BaseEntityGuid() : base()
    {
        Id = Guid.CreateVersion7();
    }

    protected BaseEntityGuid(Guid id) : base(id)
    {
    }
}
