namespace Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Base interface for domain events
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredAt { get; }
}
