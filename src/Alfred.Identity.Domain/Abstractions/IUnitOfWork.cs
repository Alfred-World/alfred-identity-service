using Alfred.Identity.Domain.Abstractions.Repositories;

namespace Alfred.Identity.Domain.Abstractions;

/// <summary>
/// Unit of Work pattern — single gateway for all repositories and transactions.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Identity
    IUserRepository Users { get; }
    ITokenRepository Tokens { get; }
    IUserLoginRepository UserLogins { get; }
    IUserBanRepository UserBans { get; }
    IUserActivityLogRepository UserActivityLogs { get; }
    IBackupCodeRepository BackupCodes { get; }

    // RBAC
    IRoleRepository Roles { get; }
    IPermissionRepository Permissions { get; }
    IRolePermissionRepository RolePermissions { get; }

    // OAuth2
    IApplicationRepository Applications { get; }
    IAuthorizationRepository Authorizations { get; }
    IScopeRepository Scopes { get; }
    ISigningKeyRepository SigningKeys { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
