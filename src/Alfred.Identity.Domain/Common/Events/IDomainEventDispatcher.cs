namespace Alfred.Identity.Domain.Common.Events;

/// <summary>
/// Dispatches domain events raised by aggregate roots.
/// Implemented in Application layer and invoked from Infrastructure UnitOfWork.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
