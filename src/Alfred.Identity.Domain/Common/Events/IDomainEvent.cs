namespace Alfred.Identity.Domain.Common.Events;

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
