namespace Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Base entity with GUID primary key for high-volume tables
/// Inherits all shared logic from BaseEntity<Guid>
/// </summary>
public abstract class BaseEntityGuid : BaseEntity<Guid>
{
    protected BaseEntityGuid() : base()
    {
        Id = Guid.NewGuid();
    }

    protected BaseEntityGuid(Guid id) : base(id)
    {
    }
}
