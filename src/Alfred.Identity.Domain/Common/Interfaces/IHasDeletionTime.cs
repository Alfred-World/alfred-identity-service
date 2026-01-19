namespace Alfred.Identity.Domain.Common.Interfaces;

/// <summary>
/// Interface for entities that support soft delete and track deletion time
/// </summary>
public interface IHasDeletionTime
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
