namespace Alfred.Identity.Domain.Common.Events;

/// <summary>
/// Interface for entities that have domain events
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
