using Alfred.Identity.Domain.Abstractions.Repositories;
using Alfred.Identity.Domain.Entities;
using Alfred.Identity.Infrastructure.Common.Abstractions;
using Alfred.Identity.Infrastructure.Repositories.Base;

using Microsoft.EntityFrameworkCore;

namespace Alfred.Identity.Infrastructure.Repositories;

/// <summary>
/// User repository implementation - inherits base repository functionality
/// </summary>
public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IDbContext context) : base(context)
    {
    }

    // Custom methods for IUserRepository
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await DbSet
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToLowerInvariant().Trim();
        return await DbSet.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalizedUsername = username.ToUpperInvariant().Trim();
        return await DbSet
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUsername, cancellationToken);
    }

    public async Task<User?> GetByIdentityAsync(string identity, CancellationToken cancellationToken = default)
    {
        var normalizedIdentity = identity.Trim();

        // Try to find by email first (if it looks like an email)
        if (identity.Contains('@'))
        {
            return await GetByEmailAsync(normalizedIdentity, cancellationToken);
        }

        // Otherwise try to find by username
        return await GetByUsernameAsync(normalizedIdentity, cancellationToken);
    }

    public async Task<User?> GetByIdWithRolesAsync(long id, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }
}
