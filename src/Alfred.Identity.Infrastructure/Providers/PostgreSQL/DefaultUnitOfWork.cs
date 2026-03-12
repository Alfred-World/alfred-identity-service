using Alfred.Identity.Domain.Abstractions;
using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Alfred.Identity.Infrastructure.Providers.PostgreSQL;

/// <summary>
/// Default implementation of Unit of Work pattern.
/// All repositories are lazy-loaded and share the same DbContext.
/// </summary>
public class DefaultUnitOfWork : IUnitOfWork
{
    private readonly IDbContext _context;

    // Identity
    private IUserRepository? _users;
    private ITokenRepository? _tokens;
    private IUserLoginRepository? _userLogins;
    private IUserBanRepository? _userBans;
    private IUserActivityLogRepository? _userActivityLogs;
    private IBackupCodeRepository? _backupCodes;

    // RBAC
    private IRoleRepository? _roles;
    private IPermissionRepository? _permissions;
    private IRolePermissionRepository? _rolePermissions;

    // OAuth2
    private IApplicationRepository? _applications;
    private IAuthorizationRepository? _authorizations;
    private IScopeRepository? _scopes;
    private ISigningKeyRepository? _signingKeys;

    public DefaultUnitOfWork(IDbContext context)
    {
        _context = context;
    }

    // Identity
    public IUserRepository Users => _users ??= new UserRepository(_context);
    public ITokenRepository Tokens => _tokens ??= new TokenRepository(_context);
    public IUserLoginRepository UserLogins => _userLogins ??= new UserLoginRepository(_context);
    public IUserBanRepository UserBans => _userBans ??= new UserBanRepository(_context);
    public IUserActivityLogRepository UserActivityLogs => _userActivityLogs ??= new UserActivityLogRepository(_context);
    public IBackupCodeRepository BackupCodes => _backupCodes ??= new BackupCodeRepository(_context);

    // RBAC
    public IRoleRepository Roles => _roles ??= new RoleRepository(_context);
    public IPermissionRepository Permissions => _permissions ??= new PermissionRepository(_context);
    public IRolePermissionRepository RolePermissions => _rolePermissions ??= new RolePermissionRepository(_context);

    // OAuth2
    public IApplicationRepository Applications => _applications ??= new ApplicationRepository(_context);
    public IAuthorizationRepository Authorizations => _authorizations ??= new AuthorizationRepository(_context);
    public IScopeRepository Scopes => _scopes ??= new ScopeRepository(_context);
    public ISigningKeyRepository SigningKeys => _signingKeys ??= new SigningKeyRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_context is DbContext dbContext)
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }

        throw new InvalidOperationException("DbContext is not available");
    }

    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        if (_context is not DbContext dbContext)
        {
            throw new InvalidOperationException("DbContext is not available");
        }

        IDbContextTransaction? transaction = null;
        try
        {
            transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public void Dispose()
    {
        if (_context is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
