namespace Alfred.Identity.Domain.Common.Base;

/// <summary>
/// Base class for domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;
    public Guid EventId { get; init; } = Guid.NewGuid();
}
