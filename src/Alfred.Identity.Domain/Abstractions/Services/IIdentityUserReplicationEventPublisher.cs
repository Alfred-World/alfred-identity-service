namespace Alfred.Identity.Domain.Abstractions.Services;

public interface IIdentityUserReplicationEventPublisher
{
    Task PublishUserUpsertedAsync(Guid userId, string userName, string email, string fullName, string? avatar,
        string status,
        bool isBanned,
        bool isDeleted,
        CancellationToken cancellationToken = default);

    Task PublishUserDeletedAsync(Guid userId, CancellationToken cancellationToken = default);
}