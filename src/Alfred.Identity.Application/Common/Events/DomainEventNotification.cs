using Alfred.Identity.Domain.Common.Events;

using MediatR;

namespace Alfred.Identity.Application.Common.Events;

public sealed record DomainEventNotification(IDomainEvent DomainEvent) : INotification;
