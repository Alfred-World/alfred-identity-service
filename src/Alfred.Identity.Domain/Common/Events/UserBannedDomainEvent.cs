using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Common.Events;

public sealed record UserBannedDomainEvent(UserId UserId, DateTime? ExpiresAt) : DomainEvent;
