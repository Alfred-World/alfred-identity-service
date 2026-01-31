namespace Alfred.Identity.Domain.Common.Interfaces;

/// <summary>
/// Interface for entities that track who created them
/// </summary>
public interface IHasCreator
{
    Guid? CreatedById { get; set; }
}
