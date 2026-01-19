namespace Alfred.Identity.Domain.Common.Interfaces;

/// <summary>
/// Interface for entities that track who deleted them
/// </summary>
public interface IHasDeleter
{
    long? DeletedById { get; set; }
}
