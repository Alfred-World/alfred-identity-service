namespace Alfred.Identity.Domain.Common.Interfaces;

/// <summary>
/// Interface for entities that track modification time
/// </summary>
public interface IHasModificationTime
{
    DateTime? UpdatedAt { get; set; }
}
