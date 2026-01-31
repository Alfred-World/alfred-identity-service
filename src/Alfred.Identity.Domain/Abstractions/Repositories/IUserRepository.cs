using Alfred.Identity.Domain.Entities;

namespace Alfred.Identity.Domain.Abstractions.Repositories;

/// <summary>
/// Repository interface for User entity - extends base IRepository
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Get user by email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by username
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by identity (email or username)
    /// </summary>
    Task<User?> GetByIdentityAsync(string identity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user by ID including roles
    /// </summary>
    Task<User?> GetByIdWithRolesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if email exists
    /// </summary>
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);


}
