namespace Alfred.Identity.Domain.Common.Interfaces;

/// <summary>
/// Interface for entities that track who created them
/// </summary>
public interface IHasCreator
{
    long? CreatedById { get; set; }
}
