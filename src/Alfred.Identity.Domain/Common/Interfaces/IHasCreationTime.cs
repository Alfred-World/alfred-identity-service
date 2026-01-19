namespace Alfred.Identity.Domain.Common.Interfaces;

/// <summary>
/// Interface for entities that track creation time
/// </summary>
public interface IHasCreationTime
{
    DateTime CreatedAt { get; set; }
}
