using System.Buffers.Binary;

using Alfred.Identity.Domain.Abstractions.Services;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Alfred.Identity.Infrastructure.Services;

public sealed class IdentityUserReplicationEventPublisher : IIdentityUserReplicationEventPublisher
{
    private const string DefaultStreamKey = "identity:user-events";

    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<IdentityUserReplicationEventPublisher> _logger;

    public IdentityUserReplicationEventPublisher(
        IConnectionMultiplexer redis,
        ILogger<IdentityUserReplicationEventPublisher> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task PublishUserUpsertedAsync(Guid userId, string userName, string email, string fullName, string? avatar,
        string status,
        bool isBanned,
        bool isDeleted,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        await PublishAsync("UPSERT", userId, userName, email, fullName, avatar, status, isBanned, isDeleted);
    }

    public async Task PublishUserDeletedAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await PublishAsync("DELETE", userId, null, null, null, null, null, null, null);
    }

    private async Task PublishAsync(string action, Guid userId, string? userName, string? email, string? fullName,
        string? avatar, string? status, bool? isBanned, bool? isDeleted)
    {
        try
        {
            var streamKey = Environment.GetEnvironmentVariable("IDENTITY_USER_STREAM_KEY") ?? DefaultStreamKey;
            var db = _redis.GetDatabase();
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            var message = new List<NameValueEntry>
            {
                new("action", action),
                new("eventType", action),
                new("timestamp", timestamp.ToString()),
                new("userId", MapGuidToPositiveInt64(userId).ToString()),
                new("userGuid", userId.ToString())
            };

            if (!string.IsNullOrWhiteSpace(userName))
            {
                message.Add(new NameValueEntry("userName", userName));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                message.Add(new NameValueEntry("email", email));
            }

            if (!string.IsNullOrWhiteSpace(fullName))
            {
                message.Add(new NameValueEntry("fullName", fullName));
            }

            if (!string.IsNullOrWhiteSpace(avatar))
            {
                message.Add(new NameValueEntry("avatar", avatar));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                message.Add(new NameValueEntry("status", status));
            }

            if (isBanned.HasValue)
            {
                message.Add(new NameValueEntry("isBanned", isBanned.Value ? "true" : "false"));
            }

            if (isDeleted.HasValue)
            {
                message.Add(new NameValueEntry("isDeleted", isDeleted.Value ? "true" : "false"));
            }

            _ = await db.StreamAddAsync(streamKey, message.ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish identity user replication event {Action} for user {UserId}",
                action,
                userId);
        }
    }

    private static long MapGuidToPositiveInt64(Guid userId)
    {
        Span<byte> bytes = stackalloc byte[16];
        userId.TryWriteBytes(bytes);

        var value = BinaryPrimitives.ReadInt64LittleEndian(bytes[..8]);

        if (value == long.MinValue)
        {
            return long.MaxValue;
        }

        return Math.Abs(value);
    }
}