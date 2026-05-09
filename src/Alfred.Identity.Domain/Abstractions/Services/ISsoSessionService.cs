using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Services;

public interface ISsoSessionService
{
    string SsoSessionClaimType { get; }

    string HashSessionId(string sessionId);

    Task<CreateSsoSessionResult> CreateAsync(
        UserId userId,
        bool rememberMe,
        DateTimeOffset expiresUtc,
        string? ipAddress,
        string? device,
        CancellationToken cancellationToken = default);

    Task<SsoSessionValidationResult> ValidateAsync(
        string sessionId,
        UserId userId,
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        string sessionId,
        UserId? userId = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<bool> RevokeByIdAsync(
        TokenId tokenId,
        UserId userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<int> RevokeAllByUserAsync(
        UserId userId,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Token>> GetActiveSessionsByUserAsync(
        UserId userId,
        CancellationToken cancellationToken = default);
}

public sealed record CreateSsoSessionResult(string SessionId, Token Token);

public sealed record SsoSessionValidationResult(bool IsValid, Token? Token = null, string? Error = null);
