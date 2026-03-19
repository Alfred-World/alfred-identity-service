using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Common.Events;

public sealed record UserUnbannedDomainEvent(UserId UserId) : DomainEvent;
