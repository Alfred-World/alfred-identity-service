namespace Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Base entity with generic TId for flexible ID types
/// Contains only Id and basic comparison logic (SOLID - Single Responsibility)
/// Use Entity, FullAuditedEntity, or BasicAuditedEntity for audit fields
/// </summary>
public abstract class BaseEntity<TId> where TId : IEquatable<TId>
{
    public TId Id { get; protected set; } = default!;

    protected BaseEntity()
    {
    }

    protected BaseEntity(TId id)
    {
        Id = id;
    }

    protected virtual bool IsTransient()
    {
        return Id == null || Id.Equals(default);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity<TId> other)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (GetType() != other.GetType())
        {
            return false;
        }

        if (IsTransient() || other.IsTransient())
        {
            return false;
        }

        return Id.Equals(other.Id);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(GetType(), Id);
    }

    public static bool operator ==(BaseEntity<TId>? a, BaseEntity<TId>? b)
    {
        if (a is null && b is null)
        {
            return true;
        }

        if (a is null || b is null)
        {
            return false;
        }

        return a.Equals(b);
    }

    public static bool operator !=(BaseEntity<TId>? a, BaseEntity<TId>? b)
    {
        return !(a == b);
    }
}

/// <summary>
/// Base entity with long ID (most common case)
/// </summary>
public abstract class BaseEntity : BaseEntity<long>
{
    protected BaseEntity() : base()
    {
    }

    protected BaseEntity(long id) : base(id)
    {
    }
}
