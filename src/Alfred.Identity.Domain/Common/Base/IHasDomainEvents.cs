namespace Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Interface for entities that have domain events
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
